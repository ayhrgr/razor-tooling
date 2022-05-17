﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class RazorCompletionEndpoint : IVSCompletionEndpoint
    {
        private static readonly VSInternalCompletionList s_EmptyCompletionList = new VSInternalCompletionList()
        {
            Items = Array.Empty<CompletionItem>(),
        };
        private readonly ILogger _logger;
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorCompletionFactsService _completionFactsService;
        private readonly CompletionListCache _completionListCache;
        private readonly DelegatedCompletionService _delegatedCompletionService;
        private readonly DocumentVersionCache _documentVersionCache;
        private static readonly Command s_retriggerCompletionCommand = new()
        {
            CommandIdentifier = "editor.action.triggerSuggest",
            Title = RazorLS.Resources.ReTrigger_Completions_Title,
        };
        private VSInternalClientCapabilities? _clientCapabilities;

        public RazorCompletionEndpoint(
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            DocumentResolver documentResolver,
            RazorCompletionFactsService completionFactsService,
            CompletionListCache completionListCache,
            DelegatedCompletionService delegatedCompletionService,
            DocumentVersionCache documentVersionCache,
            ILoggerFactory loggerFactory)
        {
            if (projectSnapshotManagerDispatcher is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            }

            if (documentResolver is null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (completionFactsService is null)
            {
                throw new ArgumentNullException(nameof(completionFactsService));
            }

            if (completionListCache is null)
            {
                throw new ArgumentNullException(nameof(completionListCache));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _documentResolver = documentResolver;
            _completionFactsService = completionFactsService;
            _logger = loggerFactory.CreateLogger<RazorCompletionEndpoint>();
            _completionListCache = completionListCache;
            _delegatedCompletionService = delegatedCompletionService;
            _documentVersionCache = documentVersionCache;
        }

        public RegistrationExtensionResult? GetRegistration(VSInternalClientCapabilities clientCapabilities)
        {
            const string AssociatedServerCapability = "completionProvider";

            _clientCapabilities = clientCapabilities;

            var registrationOptions = new CompletionOptions()
            {
                ResolveProvider = true,
                TriggerCharacters = new[] { "@", "<", ":", "." },
                AllCommitCharacters = new[] { " ", "{", "}", "[", "]", "(", ")", ".", ",", ":", ";", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "<", ">", "?", "@", "#", "'", "\"", "\\" },
            };

            return new RegistrationExtensionResult(AssociatedServerCapability, registrationOptions);
        }

        public async Task<VSInternalCompletionList?> Handle(VSCompletionParamsBridge request, CancellationToken cancellationToken)
        {
            var documentAndSnapshot = await TryGetDocumentSnapshotAndVersionAsync(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), cancellationToken).ConfigureAwait(false);
            if (documentAndSnapshot is null || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var document = documentAndSnapshot.Snapshot;
            if (request.Context is null || !IsApplicableTriggerContext(request.Context))
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();

            var sourceText = await document.GetTextAsync();
            if (!request.Position.TryGetAbsoluteIndex(sourceText, _logger, out var hostDocumentIndex))
            {
                return null;
            }

            var location = new SourceSpan(hostDocumentIndex, 0);
            var reason = request.Context.TriggerKind switch
            {
                CompletionTriggerKind.TriggerForIncompleteCompletions => CompletionReason.Invoked,
                CompletionTriggerKind.Invoked => CompletionReason.Invoked,
                CompletionTriggerKind.TriggerCharacter => CompletionReason.Typing,
                _ => CompletionReason.Typing,
            };
            var completionOptions = new RazorCompletionOptions(SnippetsSupported: true);
            var completionContext = new RazorCompletionContext(syntaxTree, tagHelperDocumentContext, reason, completionOptions);

            var razorCompletionItems = _completionFactsService.GetCompletionItems(completionContext, location) ?? Array.Empty<RazorCompletionItem>();

            _logger.LogTrace($"Resolved {razorCompletionItems.Count} completion items.");
            var delegatedCompletionResult = await _delegatedCompletionService.GetCompletionListAsync(request, document, documentAndSnapshot.Version, cancellationToken).ConfigureAwait(false);
            var resultId = _completionListCache.Set(razorCompletionItems, delegatedCompletionResult);
            var razorCompletionList = CreateLSPCompletionList(razorCompletionItems);
            var delegatedCompletionList = delegatedCompletionResult?.CompletionList ?? s_EmptyCompletionList;
            var completionCapability = _clientCapabilities?.TextDocument?.Completion as VSInternalCompletionSetting;
            razorCompletionList.SetResultId(resultId, completionCapability);
            
            var optimizedRazorCompletionList = CompletionListOptimizer.Optimize(razorCompletionList, completionCapability);
            var mergedCompletionList = CompletionListMerger.Merge(delegatedCompletionList, optimizedRazorCompletionList);
            return mergedCompletionList;
        }

        // Internal for testing
        internal static bool IsApplicableTriggerContext(CompletionContext context)
        {
            if (context is not VSInternalCompletionContext vsCompletionContext)
            {
                Debug.Fail("Completion context should always be converted into a VSCompletionContext (even in VSCode).");

                // We do not support providing completions on delete.
                return false;
            }

            if (vsCompletionContext.InvokeKind == VSInternalCompletionInvokeKind.Deletion)
            {
                // We do not support providing completions on delete.
                return false;
            }

            return true;
        }

        // Internal for testing
        internal VSInternalCompletionList CreateLSPCompletionList(IReadOnlyList<RazorCompletionItem> razorCompletionItems) => CreateLSPCompletionList(razorCompletionItems, _clientCapabilities!);

        // Internal for benchmarking and testing
        internal static VSInternalCompletionList CreateLSPCompletionList(
            IReadOnlyList<RazorCompletionItem> razorCompletionItems,
            VSInternalClientCapabilities clientCapabilities)
        {
            var completionItems = new List<CompletionItem>();
            foreach (var razorCompletionItem in razorCompletionItems)
            {
                if (TryConvert(razorCompletionItem, clientCapabilities, out var completionItem))
                {
                    // The completion items are cached and can be retrieved via this result id to enable the "resolve" completion functionality.
                    completionItems.Add(completionItem);
                }
            }

            var completionList = new VSInternalCompletionList()
            {
                Items = completionItems.ToArray(),
                IsIncomplete = false,
            };

            return completionList;
        }

        // Internal for testing
        internal static bool TryConvert(
            RazorCompletionItem razorCompletionItem,
            VSInternalClientCapabilities clientCapabilities,
            [NotNullWhen(true)] out VSInternalCompletionItem? completionItem)
        {
            if (razorCompletionItem is null)
            {
                throw new ArgumentNullException(nameof(razorCompletionItem));
            }

            var tagHelperCompletionItemKind = CompletionItemKind.TypeParameter;
            var supportedItemKinds = clientCapabilities.TextDocument?.Completion?.CompletionItemKind?.ValueSet ?? Array.Empty<CompletionItemKind>();
            if (supportedItemKinds?.Contains(CompletionItemKind.TagHelper) == true)
            {
                tagHelperCompletionItemKind = CompletionItemKind.TagHelper;
            }

            var insertTextFormat = razorCompletionItem.IsSnippet ? InsertTextFormat.Snippet : InsertTextFormat.Plaintext;

            switch (razorCompletionItem.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        var directiveCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = CompletionItemKind.Struct,
                        };

                        directiveCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        if (razorCompletionItem == DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem)
                        {
                            directiveCompletionItem.Command = s_retriggerCompletionCommand;
                            directiveCompletionItem.Kind = tagHelperCompletionItemKind;
                        }

                        completionItem = directiveCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttribute:
                    {
                        var directiveAttributeCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = tagHelperCompletionItemKind,
                        };

                        directiveAttributeCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        completionItem = directiveAttributeCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttributeParameter:
                    {
                        var parameterCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = tagHelperCompletionItemKind,
                        };

                        parameterCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        completionItem = parameterCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.MarkupTransition:
                    {
                        var markupTransitionCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = tagHelperCompletionItemKind,
                        };

                        markupTransitionCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        completionItem = markupTransitionCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.TagHelperElement:
                    {
                        var tagHelperElementCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = tagHelperCompletionItemKind,
                        };

                        tagHelperElementCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        completionItem = tagHelperElementCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.TagHelperAttribute:
                    {
                        var tagHelperAttributeCompletionItem = new VSInternalCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.SortText,
                            InsertTextFormat = insertTextFormat,
                            Kind = tagHelperCompletionItemKind,
                        };

                        tagHelperAttributeCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                        completionItem = tagHelperAttributeCompletionItem;
                        return true;
                    }
            }

            completionItem = null;
            return false;
        }

        private record DocumentSnapshotAndVersion(DocumentSnapshot Snapshot, int Version);

        private Task<DocumentSnapshotAndVersion?> TryGetDocumentSnapshotAndVersionAsync(string uri, CancellationToken cancellationToken)
        {
            return _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(() =>
            {
                if (_documentResolver.TryResolveDocument(uri, out var documentSnapshot))
                {
                    if (_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out var version))
                    {
                        return new DocumentSnapshotAndVersion(documentSnapshot, version.Value);
                    }
                }

                return null;
            }, cancellationToken);
        }
    }
}
