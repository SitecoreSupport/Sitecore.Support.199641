// (c) 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web.UI;
using Sitecore.ContentTesting.Caching;
using Sitecore.ContentTesting.Data;
using Sitecore.ContentTesting.Model.Data.Items;
using Sitecore.ContentTesting.Models;
using Sitecore.Data;
using Sitecore.Data.Comparers;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Resources;
using Sitecore.Shell.Applications.Dialogs.Testing;
using Sitecore.Shell.Controls;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.ContentTesting.Shell.Applications.Dialogs
{
    /// <summary>The set test details form.</summary>
    public class SetTestDetailsForm : DialogForm
    {
        #region Constants and Fields

        /// <summary>
        /// The component replacing
        /// </summary>
        protected Checkbox ComponentReplacing;

        /// <summary>
        /// New variation
        /// </summary>
        protected Button NewVariation;

        /// <summary>
        /// The grid Panel
        /// </summary>
        protected Border NoVariations;

        /// <summary>
        /// Reset Container
        /// </summary>
        protected Border ResetContainer;

        /// <summary>
        /// The container
        /// </summary>
        protected Border Variations;

        /// <summary>
        /// The new variation default name;
        /// </summary>        
        private static readonly string NewVariationDefaultName = Sitecore.Texts.Newvariation;

        /// <summary>The device.</summary>
        private DeviceDefinition device;

        /// <summary>The layout.</summary>
        private LayoutDefinition layout;

        /// <summary>The rendering.</summary>
        private RenderingDefinition rendering;

        /// <summary>The <see cref="SitecoreContentTestStore"/> to create the test in.</summary>
        private readonly SitecoreContentTestStore contentTestStore = null;

        #endregion

        #region Properties

        /// <summary>Gets or sets ItemId.</summary>
        [CanBeNull]
        protected ItemUri ContextItemUri
        {
            get
            {
                return ServerProperties["itemUri"] as ItemUri;
            }

            set
            {
                ServerProperties["itemUri"] = value;
            }
        }

        /// <summary>Gets Device.</summary>
        [CanBeNull]
        protected DeviceDefinition Device
        {
            get
            {
                if (device == null)
                {
                    var layoutValue = Layout;
                    if (layoutValue != null && !string.IsNullOrEmpty(DeviceId))
                    {
                        device = layoutValue.GetDevice(DeviceId);
                    }
                }

                return device;
            }
        }

        /// <summary>Gets or sets DeviceId.</summary>
        [CanBeNull]
        protected string DeviceId
        {
            get
            {
                return ServerProperties["deviceid"] as string;
            }

            set
            {
                Assert.IsNotNullOrEmpty(value, "value");
                ServerProperties["deviceid"] = value;
            }
        }

        /// <summary>Gets Layout.</summary>
        [CanBeNull]
        protected LayoutDefinition Layout
        {
            get
            {
                if (layout == null)
                {
                    var sessionValue = WebUtil.GetSessionString(LayoutSessionHandle);
                    if (!string.IsNullOrEmpty(sessionValue))
                    {
                        layout = LayoutDefinition.Parse(sessionValue);
                    }
                }

                return layout;
            }
        }

        /// <summary>Gets or sets LayoutSessionHandle.</summary>
        [CanBeNull]
        protected string LayoutSessionHandle
        {
            get
            {
                return ServerProperties["lsh"] as string;
            }

            set
            {
                Assert.IsNotNullOrEmpty(value, "value");
                ServerProperties["lsh"] = value;
            }
        }

        /// <summary>
        /// Gets the rendering.
        /// </summary>
        /// <value>The rendering.</value>
        [CanBeNull]
        protected RenderingDefinition Rendering
        {
            get
            {
                if (rendering == null)
                {
                    DeviceDefinition device = Device;
                    var id = RenderingUniqueId;
                    if (device != null && !string.IsNullOrEmpty(id))
                    {
                        rendering = device.GetRenderingByUniqueId(id);
                    }
                }

                return rendering;
            }
        }

        /// <summary>Gets or sets RenderingUniqueId.</summary>
        [CanBeNull]
        protected string RenderingUniqueId
        {
            get
            {
                return ServerProperties["renderingid"] as string;
            }

            set
            {
                Assert.IsNotNullOrEmpty(value, "value");
                ServerProperties["renderingid"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the variables.
        /// </summary>
        /// <value>The variables.</value>
        [NotNull]
        protected List<VariableValueItemStub> VariableValues
        {
            get
            {
                var value = ServerProperties["variables"] as List<VariableValueItemStub>;
                return value ?? new List<VariableValueItemStub>();
            }

            set
            {
                Assert.IsNotNull(value, "value");
                ServerProperties["variables"] = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SetTestDetailsForm"/> type.
        /// </summary>
        public SetTestDetailsForm()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetTestDetailsForm"/> type.
        /// </summary>
        /// <param name="testStore">The <see cref="SitecoreContentTestStore"/> to create the test in.</param>
        public SetTestDetailsForm([CanBeNull] SitecoreContentTestStore testStore)
        {
            contentTestStore =
              testStore ??
              ContentTestingFactory.Instance.ContentTestStore as SitecoreContentTestStore ??
              new SitecoreContentTestStore();
        }
        #endregion

        #region Methods

        /// <summary>Adds the variation.</summary>
        [UsedImplicitly]
        protected void AddVariation()
        {
            var id = ID.NewID;
            var variation = new VariableValueItemStub(id, Translate.Text(NewVariationDefaultName));
            var values = VariableValues;
            values.Insert(0, variation);
            VariableValues = values;
            var html = RenderVariableValue(variation);
            SetControlsState();
            SheerResponse.Insert(Variations.ClientID, "afterBegin", html);
            SheerResponse.Eval("Sitecore.CollapsiblePanel.newAdded('{0}')".FormatWith(id.ToShortID()));
        }

        /// <summary>
        /// Allows the component replace.
        /// </summary>
        protected void AllowComponentReplace()
        {
            if (!ComponentReplacing.Checked)
            {
                if (VariableValues.FindIndex(v => !string.IsNullOrEmpty(v.ReplacementComponent)) >= 0)
                {
                    var parameters = new NameValueCollection();
                    Context.ClientPage.Start(this, "ShowConfirm", parameters);
                    return;
                }
            }

            SheerResponse.Eval("scToggleTestComponentSection()");
        }

        /// <summary>Changes the display component.</summary>
        /// <param name="variationId">The variation id.</param>
        [UsedImplicitly]
        protected void ChangeDisplayComponent([NotNull] string variationId)
        {
            Assert.ArgumentNotNull(variationId, "variationId");

            var id = ShortID.DecodeID(variationId);
            var values = VariableValues;
            var value = values.Find(v => v.Id == id);

            if (value == null)
            {
                return;
            }

            value.HideComponent = !value.HideComponent;
            using (var writer = new HtmlTextWriter(new StringWriter()))
            {
                RenderContentControls(writer, value);
                SheerResponse.SetOuterHtml(variationId + "_content", writer.InnerWriter.ToString());
            }

            using (var writer = new HtmlTextWriter(new StringWriter()))
            {
                RenderComponentControls(writer, value);
                SheerResponse.SetOuterHtml(variationId + "_component", writer.InnerWriter.ToString());
            }

            VariableValues = values;
        }

        /// <summary>Inits the variable values.</summary>
        protected virtual void InitVariableValues()
        {
            if (Rendering == null)
            {
                return;
            }

            var testVariable = contentTestStore.GetMultivariateTestVariable(Rendering, ContextItemUri.Language, Client.ContentDatabase);
            if (testVariable == null)
            {
                return;
            }

            var values = testVariable.Values;
            var stubs = new List<VariableValueItemStub>();

            foreach (var value in values)
            {
                var stub = new VariableValueItemStub(value.ID, value["Name"])
                {
                    Datasource = value.Datasource.Uri != null && value.Datasource.Uri.ItemID != (ID)null ?
                      value.Datasource.Uri.ItemID.ToString() : string.Empty,
                    HideComponent = value.HideComponent,
                    IsOriginal = value.IsOriginal
                };

                stub.ReplacementComponent = value.ReplacementComponent.Uri != null && value.ReplacementComponent.Uri.ItemID != (ID)null ?
                  value.ReplacementComponent.Uri.ItemID.ToString() : string.Empty;

                stubs.Add(stub);
            }

            VariableValues = stubs;
        }

        /// <summary>The on load.</summary>
        /// <param name="e">The e.</param>
        protected override void OnLoad([NotNull] EventArgs e)
        {
            base.OnLoad(e);

            if (Context.ClientPage.IsEvent)
            {
                return;
            }

            var options = SetTestDetailsOptions.Parse();
            DeviceId = options.DeviceId;
            ContextItemUri = ItemUri.Parse(options.ItemUri);
            RenderingUniqueId = options.RenderingUniqueId;
            LayoutSessionHandle = options.LayoutSessionHandle;
            InitVariableValues();
            if (VariableValues.FindIndex(v => !string.IsNullOrEmpty(v.ReplacementComponent)) > -1)
            {
                ComponentReplacing.Checked = true;
            }
            else
            {
                Variations.Class = "hide-test-component";
            }

            if (VariableValues.Count > 0)
            {
                ResetContainer.Visible = true;
            }

            var values = VariableValues;
            if (VariableValues.Count == 0)
            {
                var id = ID.NewID;
                var variation = new VariableValueItemStub(id, Translate.Text(Sitecore.Texts.Original));

                if (Rendering != null &&
                    Rendering.Datasource != ContextItemUri.ItemID.ToString() &&
                    !string.IsNullOrEmpty(Rendering.Datasource))
                {
                    variation.Datasource = Rendering.Datasource;
                }
                else
                {
                    variation.Datasource = ContextItemUri.ItemID.ToString();
                }

                variation.IsOriginal = true;

                values.Insert(0, variation);
                VariableValues = values;
                var html = RenderVariableValue(variation);
                SetControlsState();
                SheerResponse.Insert(Variations.ClientID, "afterBegin", html);
                SheerResponse.Eval("Sitecore.CollapsiblePanel.newAdded('{0}')".FormatWith(id.ToShortID()));

            }
            if (Rendering != null)
            {
                Item variable = contentTestStore.GetMultivariateTestVariable(Rendering, ContextItemUri.Language, Client.ContentDatabase);
                if (variable != null && !variable.Access.CanCreate())
                {
                    NewVariation.Disabled = true;
                }
            }

            SetControlsState();
            Render();
        }

        /// <summary>The on ok.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected override void OnOK([NotNull] object sender, [NotNull] EventArgs args)
        {
            var device = Device;
            Assert.IsNotNull(device, "device");

            TestDefinitionItem testDefinition = null;

            var testVariable = contentTestStore.GetMultivariateTestVariable(Rendering, ContextItemUri.Language, Client.ContentDatabase);
            if (testVariable != null)
            {
                testDefinition = testVariable.TestDefinition;
            }

            if (testDefinition == null)
            {
                var itemUri = ContextItemUri;
                if (itemUri == null)
                {
                    return;
                }

                var contextItem = Client.ContentDatabase.GetItem(itemUri.ToDataUri());
                if (contextItem != null)
                {
                    testDefinition = contentTestStore.AddTestDefinition(contextItem);
                }
            }

            if (testDefinition == null)
            {
                SheerResponse.Alert(Sitecore.Texts.TheActionCannotBeExecuted);
                return;
            }
            else
            {
                using (new EditContext(testDefinition))
                {
                    testDefinition.Device.Value = device.ID;
                }
            }

            if (Rendering == null)
            {
                return;
            }

            var variableItem =
              contentTestStore.GetMultivariateTestVariable(Rendering, ContextItemUri.Language, Client.ContentDatabase) ??
              testDefinition.AddMultivariateTestVariable(Rendering);

            if (variableItem == null)
            {
                SheerResponse.Alert(Sitecore.Texts.TheActionCannotBeExecuted);
                return;
            }

            List<ID> modifiedIds;
            var success = UpdateVariableValues(variableItem, out modifiedIds);
            if (!success)
            {
                SheerResponse.Alert(Sitecore.Texts.TheActionCannotBeExecuted);
                return;
            }

            var result = SetTestDetailsOptions.GetDialogResut(variableItem.ID, modifiedIds);
            SheerResponse.SetDialogValue(result);
            SheerResponse.CloseWindow();
        }

        /// <summary>Removes the variation.</summary>
        /// <param name="variationId">The variation id.</param>
        [UsedImplicitly]
        protected void RemoveVariation([NotNull] string variationId)
        {
            Assert.ArgumentNotNull(variationId, "variationId");

            var id = ShortID.DecodeID(variationId);
            var values = VariableValues;
            var idx = values.FindIndex(value => value.Id == id);
            if (idx < 0)
            {
                SheerResponse.Alert(Sitecore.Texts.ITEM_NOT_FOUND);
                return;
            }

            values.RemoveAt(idx);
            SheerResponse.Remove(variationId);
            VariableValues = values;
            SetControlsState();
        }

        /// <summary>Renames the variation.</summary>
        /// <param name="message">The message.</param>    
        [UsedImplicitly]
        [HandleMessage("variation:rename")]
        protected void RenameVariation([NotNull] Message message)
        {
            var variationId = message.Arguments["variationId"];
            var name = message.Arguments["name"];
            Assert.ArgumentNotNull(variationId, "variationId");
            Assert.ArgumentNotNull(name, "name");

            var id = ShortID.DecodeID(variationId);
            var values = VariableValues;
            var idx = values.FindIndex(value => value.Id == id);
            if (idx < 0)
            {
                SheerResponse.Alert(Sitecore.Texts.ITEM_NOT_FOUND);
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                SheerResponse.Alert(Sitecore.Texts.AN_ITEM_NAME_MAY_NOT_BE_BLANK);
                SheerResponse.Eval("Sitecore.CollapsiblePanel.editName(\"{0}\")".FormatWith(variationId));
                return;
            }

            values[idx].Name = name;
            VariableValues = values;
        }

        /// <summary>Renders this instance.</summary>
        protected virtual void Render()
        {
            var output = new HtmlTextWriter(new StringWriter());
            foreach (VariableValueItemStub value in VariableValues)
            {
                output.Write(RenderVariableValue(value));
            }

            var html = output.InnerWriter.ToString();
            if (!string.IsNullOrEmpty(html))
            {
                Variations.InnerHtml = html;
            }
        }

        /// <summary>
        /// Handles the Reset_ click event.
        /// </summary>
        [HandleMessage("variation:reset", true)]
        protected void Reset_Click([NotNull] ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (!args.HasResult || args.Result == "no")
                {
                    return;
                }

                var rendering = Rendering;
                var dialogValue = "#reset#";
                if (rendering != null)
                {
                    var variableItem = contentTestStore.GetMultivariateTestVariable(rendering, ContextItemUri.Language, Client.ContentDatabase);
                    if (variableItem == null)
                    {
                        SheerResponse.SetDialogValue(dialogValue);
                        SheerResponse.CloseWindow();
                        return;
                    }

                    if (!variableItem.InnerItem.Access.CanDelete())
                    {
                        SheerResponse.Alert(Sitecore.Texts.TheActionCannotBeExecuted);
                        return;
                    }

                    var testDefinition = variableItem.InnerItem.Parent;
                    variableItem.InnerItem.Delete();
                    if (testDefinition != null && testDefinition.Access.CanDelete() && !testDefinition.HasChildren)
                    {
                        testDefinition.Delete();
                    }

                    ActiveTestCache.Instance.Clear(ContextItemUri.DatabaseName, ContextItemUri.ItemID);
                }

                SheerResponse.SetDialogValue(dialogValue);
                SheerResponse.CloseWindow();
            }
            else
            {
                SheerResponse.Confirm(Sitecore.Texts.ComponentwillberemovedfromthetestsetAreyousureyouwanttocontinue);
                args.WaitForPostBack();
            }
        }

        /// <summary>Resets the content of the variation.</summary>
        /// <param name="variationId">The variation id.</param>
        protected void ResetVariationContent([NotNull] string variationId)
        {
            Assert.ArgumentNotNull(variationId, "variationId");

            var id = ShortID.DecodeID(variationId);
            var values = VariableValues;
            var value = values.Find(v => v.Id == id);
            if (value != null)
            {
                value.Datasource = string.Empty;
                var writer = new HtmlTextWriter(new StringWriter());
                RenderContentControls(writer, value);
                SheerResponse.SetOuterHtml(variationId + "_content", writer.InnerWriter.ToString());
                VariableValues = values;
            }
        }

        /// <summary>Resets the content of the variation.</summary>
        /// <param name="variationId">The variation id.</param>
        protected void ResetVariationComponent([NotNull] string variationId)
        {
            Assert.ArgumentNotNull(variationId, "variationId");

            var id = ShortID.DecodeID(variationId);
            var values = VariableValues;
            var value = values.Find(v => v.Id == id);
            if (value != null)
            {
                value.ReplacementComponent = string.Empty;
                var writer = new HtmlTextWriter(new StringWriter());
                RenderComponentControls(writer, value);
                SheerResponse.SetOuterHtml(variationId + "_component", writer.InnerWriter.ToString());
                VariableValues = values;
            }
        }

        /// <summary>
        /// Sets the component.
        /// </summary>
        /// <param name="args">The arguments.</param>
        [HandleMessage("variation:setcomponent", true)]
        protected void SetComponent([NotNull] ClientPipelineArgs args)
        {
            var variationId = args.Parameters["variationid"];
            if (string.IsNullOrEmpty(variationId))
            {
                SheerResponse.Alert(Sitecore.Texts.ITEM_NOT_FOUND);
                return;
            }

            if (Rendering == null || Layout == null)
            {
                SheerResponse.Alert(Sitecore.Texts.AnErrorOcurred);
                return;
            }

            if (!args.IsPostBack)
            {
                var placeholder = Rendering.Placeholder;
                Assert.IsNotNull(placeholder, "placeholder");
                var layout = Layout.ToXml();

                var placeholderRenderingArgs = new GetPlaceholderRenderingsArgs(placeholder, layout, Client.ContentDatabase, ID.Parse(DeviceId));
                placeholderRenderingArgs.OmitNonEditableRenderings = true;
                placeholderRenderingArgs.Options.ShowOpenProperties = false;
                CorePipeline.Run("getPlaceholderRenderings", placeholderRenderingArgs);

                var url = placeholderRenderingArgs.DialogURL;
                if (string.IsNullOrEmpty(url))
                {
                    SheerResponse.Alert(Sitecore.Texts.AnErrorOcurred);
                    return;
                }

                SheerResponse.ShowModalDialog(url, "720px", "470px", string.Empty, true);
                args.WaitForPostBack();
            }
            else if (args.HasResult)
            {
                var id = ShortID.DecodeID(variationId);
                var values = VariableValues;
                var value = values.Find(v => v.Id == id);
                if (value == null)
                {
                    return;
                }

                string itemId;
                if (args.Result.IndexOf(',') >= 0)
                {
                    var parts = args.Result.Split(',');
                    itemId = parts[0];
                }
                else
                {
                    itemId = args.Result;
                }

                value.ReplacementComponent = itemId;
                var writer = new HtmlTextWriter(new StringWriter());
                RenderComponentControls(writer, value);

                SheerResponse.SetOuterHtml(variationId + "_component", writer.InnerWriter.ToString());
                VariableValues = values;
            }
        }

        /// <summary>Sets the content.</summary>
        /// <param name="args">The arguments.</param>
        [HandleMessage("variation:setcontent", true)]
        protected void SetContent([NotNull] ClientPipelineArgs args)
        {
            var variationId = args.Parameters["variationid"];
            if (string.IsNullOrEmpty(variationId))
            {
                SheerResponse.Alert(Sitecore.Texts.ITEM_NOT_FOUND);
                return;
            }

            var id = ShortID.DecodeID(variationId);
            var replacementComponent = VariableValues.First(v => v.Id.ToShortID() == ShortID.DecodeID(variationId)).ReplacementComponent;
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                {
                    return;
                }

                var values = VariableValues;
                var value = values.Find(v => v.Id == id);
                if (value == null)
                {
                    return;
                }

                value.Datasource = args.Result;
                var writer = new HtmlTextWriter(new StringWriter());
                RenderContentControls(writer, value);

                SheerResponse.SetOuterHtml(variationId + "_content", writer.InnerWriter.ToString());
                VariableValues = values;
            }
            else
            {
                var value = VariableValues.Find(v => v.Id == id);
                if (value == null)
                {
                    return;
                }

                if (Rendering == null || string.IsNullOrEmpty(Rendering.ItemID))
                {
                    return;
                }

                var renderingItem = Client.ContentDatabase.GetItem(Rendering.ItemID);
                if (renderingItem == null)
                {
                    SheerResponse.Alert(Sitecore.Texts.ITEM_NOT_FOUND);
                    return;
                }

                if (replacementComponent != null && ID.IsID(replacementComponent))
                {
                    renderingItem = Client.ContentDatabase.GetItem(new ID(replacementComponent));
                }

                var contextItem = ContextItemUri == null
                                     ? null
                                     : Client.ContentDatabase.GetItem(ContextItemUri.ToDataUri());

                var pipelineArgs = new GetRenderingDatasourceArgs(renderingItem)
                {
                    FallbackDatasourceRoots = new List<Item> { Client.ContentDatabase.GetRootItem() },
                    ContentLanguage = contextItem != null ? contextItem.Language : null,
                    ContextItemPath = contextItem != null ? contextItem.Paths.FullPath : string.Empty,
                    ShowDialogIfDatasourceSetOnRenderingItem = true,
                    CurrentDatasource = string.IsNullOrEmpty(value.Datasource) ? Rendering.Datasource : value.Datasource
                };

                CorePipeline.Run("getRenderingDatasource", pipelineArgs);
                if (string.IsNullOrEmpty(pipelineArgs.DialogUrl))
                {
                    SheerResponse.Alert(Sitecore.Texts.AnErrorOcurred);
                    return;
                }

                SheerResponse.ShowModalDialog(pipelineArgs.DialogUrl, "1200px", "700px", string.Empty, true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Shows the confirm.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void ShowConfirm([NotNull] ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (args.IsPostBack)
            {
                if (args.HasResult && args.Result != "no")
                {
                    SheerResponse.Eval("scToggleTestComponentSection()");
                    var values = VariableValues;

                    foreach (var value in values)
                    {
                        if (!string.IsNullOrEmpty(value.ReplacementComponent))
                        {
                            value.ReplacementComponent = string.Empty;
                            using (var output = new HtmlTextWriter(new StringWriter()))
                            {
                                RenderComponentControls(output, value);
                                SheerResponse.SetOuterHtml(value.Id.ToShortID() + "_component", output.InnerWriter.ToString());
                            }
                        }
                    }

                    VariableValues = values;
                }
                else
                {
                    ComponentReplacing.Checked = true;
                }
            }
            else
            {
                SheerResponse.Confirm(Sitecore.Texts.TestcomponentsettingswillberemovedAreyousureyouwanttocontinue);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Updates the variable values.
        /// </summary>
        /// <param name="variableItem">
        /// The variable item.
        /// </param>
        /// <param name="modifiedVariations">
        /// The modified Variations.
        /// </param>
        /// <returns>
        /// The value indication wether updating was successfull.
        /// </returns>
        protected virtual bool UpdateVariableValues([NotNull] MultivariateTestVariableItem variableItem, out List<ID> modifiedVariations)
        {
            Assert.ArgumentNotNull(variableItem, "variableItem");

            modifiedVariations = new List<ID>();
            var currentValues = VariableValues;
            var values = variableItem.Values;
            var originalValues = new List<MultivariateTestValueItem>(values);
            var comparer = new DefaultComparer();
            originalValues.Sort((lhs, rhs) => comparer.Compare(lhs, rhs));

            var sortOrder = originalValues.Count > 0 ? originalValues[0].InnerItem.Appearance.Sortorder - 1 : Sitecore.Configuration.Settings.DefaultSortOrder;
            var variableValueTemplateID = new TemplateID(MultivariateTestValueItem.TemplateID);
            var itemsToUpdate = new List<KeyValuePair<MultivariateTestValueItem, VariableValueItemStub>>();
            var itemsToCreate = new List<KeyValuePair<int, VariableValueItemStub>>();
            for (int i = currentValues.Count - 1; i >= 0; i--)
            {
                var current = currentValues[i];
                var currentId = current.Id;
                var idx = originalValues.FindIndex(item => item.ID == currentId);

                if (idx < 0)
                {
                    var pair = new KeyValuePair<int, VariableValueItemStub>(sortOrder--, current);
                    itemsToCreate.Add(pair);
                    continue;
                }

                var original = originalValues[idx];
                if (IsVariableValueChanged(original, current))
                {
                    itemsToUpdate.Add(new KeyValuePair<MultivariateTestValueItem, VariableValueItemStub>(original, current));
                }

                originalValues.RemoveAt(idx);
            }

            var canExecuteDeleteOperations = originalValues.Count == 0 || !originalValues.Exists(item => !item.InnerItem.Access.CanDelete());
            var canExecuteAddOperations = itemsToCreate.Count == 0 || variableItem.InnerItem.Access.CanAdd(variableValueTemplateID);
            var canExecuteUpdateOparions = itemsToUpdate.Count == 0 || !itemsToUpdate.Exists(p => !CanUpdateItem(p.Key));
            var canExecuteTransaction = canExecuteAddOperations && canExecuteUpdateOparions && canExecuteDeleteOperations;

            if (canExecuteTransaction)
            {
                foreach (Item originalValue in originalValues)
                {
                    modifiedVariations.Add(originalValue.ID);
                    originalValue.Delete();
                }

                foreach (var pair in itemsToCreate)
                {
                    var current = pair.Value;
                    var order = pair.Key;
                    string name = current.Name;

                    if (ItemUtil.ContainsNonASCIISymbols(name))
                    {
                        var template = variableItem.Database.GetItem(variableValueTemplateID.ID);
                        name = template != null ? template.Name : "Unnamed item";
                    }

                    if (!ItemUtil.IsItemNameValid(name))
                    {
                        try
                        {
                            name = ItemUtil.ProposeValidItemName(name);
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }

                    name = ItemUtil.GetUniqueName(variableItem, name);

                    var newVariableValue = variableItem.AddValue(name);
                    Assert.IsNotNull(newVariableValue, "newVariableValue");
                    UpdateVariableValueItem(newVariableValue, current, order);
                }

                foreach (var pair in itemsToUpdate)
                {
                    var item = pair.Key;
                    var stub = pair.Value;
                    modifiedVariations.Add(item.ID);
                    UpdateVariableValueItem(item, stub);
                }
            }

            return canExecuteTransaction;
        }

        /// <summary>
        /// Determines whether this instance [can update item] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can update item] the specified item; otherwise, <c>false</c>.
        /// </returns>
        private static bool CanUpdateItem([NotNull] Item item)
        {
            if (!Context.IsAdministrator && item.Locking.IsLocked() && !item.Locking.HasLock())
            {
                return false;
            }

            if (item.Appearance.ReadOnly)
            {
                return false;
            }

            return item.Access.CanWrite();
        }

        /// <summary>Determines whether [is variable value changed] [the specified variable item].</summary>
        /// <param name="variableItem">The variable item.</param>
        /// <param name="variableStub">The variable stub.</param>
        /// <returns><c>true</c> if [is variable value changed] [the specified variable item]; otherwise, <c>false</c>.</returns>
        protected static bool IsVariableValueChanged([NotNull] MultivariateTestValueItem variableItem, [NotNull] VariableValueItemStub variableStub)
        {
            Assert.ArgumentNotNull(variableItem, "variableItem");

            if (variableItem["Name"] != variableStub.Name)
            {
                return true;
            }

            if (variableItem.Datasource.Uri == null && !string.IsNullOrEmpty(variableStub.Datasource))
            {
                return true;
            }

            var dsid = ID.Null;
            ID.TryParse(variableStub.Datasource, out dsid);

            if (variableItem.Datasource.Uri != null && variableItem.Datasource.Uri.ItemID != dsid)
            {
                return true;
            }

            if (variableItem.ReplacementComponent.Uri != null && variableItem.Database.GetItem(variableItem.ReplacementComponent.Uri).Paths.FullPath != variableStub.ReplacementComponent)
            {
                return true;
            }

            if (variableItem.HideComponent != variableStub.HideComponent)
            {
                return true;
            }

            return false;
        }

        /// <summary>Sets the variable value item fields.</summary>
        /// <param name="variableValue">The variable item.</param>
        /// <param name="variableStub">The variable stub.</param>
        protected static void UpdateVariableValueItem([NotNull] MultivariateTestValueItem variableValue, [NotNull] VariableValueItemStub variableStub)
        {
            Assert.ArgumentNotNull(variableValue, "variableValue");

            UpdateVariableValueItem(variableValue, variableStub, variableValue.InnerItem.Appearance.Sortorder);
        }

        /// <summary>Sets the variable value item fields.</summary>
        /// <param name="variableValue">The variable item.</param>
        /// <param name="variableStub">The variable stub.</param>
        /// <param name="sortOrder">The sort order.</param>
        protected static void UpdateVariableValueItem(
          [NotNull] MultivariateTestValueItem variableValue, [NotNull] VariableValueItemStub variableStub, int sortOrder)
        {
            Assert.ArgumentNotNull(variableValue, "variableValue");

            using (new EditContext(variableValue))
            {
                variableValue["Name"] = variableStub.Name;
                variableValue.Datasource.Value = variableStub.Datasource;
                variableValue.HideComponent = variableStub.HideComponent;
                variableValue.ReplacementComponent.Value = variableStub.ReplacementComponent;
                variableValue.InnerItem.Appearance.Sortorder = sortOrder;
            }

            if (variableStub.IsOriginal)
            {
                var variableItem = variableValue.Variable;
                using (new EditContext(variableItem))
                {
                    variableItem.OriginalValue.Value = variableValue.ID.ToString();
                }
            }
        }

        /// <summary>The add actions menu.</summary>
        /// <param name="id">The id.</param>
        [NotNull]
        protected Menu GetActionsMenu([NotNull] string id)
        {
            Assert.IsNotNullOrEmpty(id, "id");
            var actionsMenu = new Menu();
            actionsMenu.ID = id + "_menu";
            var icon = Images.GetThemedImageSource("Office/16x16/delete.png");
            var click = "RemoveVariation(\\\"{0}\\\")".FormatWith(id);
            actionsMenu.Add(Sitecore.Texts.DELETE, icon, click);
            icon = string.Empty;
            click = "javascript:Sitecore.CollapsiblePanel.rename(this, event, \"{0}\")".FormatWith(id);
            actionsMenu.Add(Sitecore.Texts.RENAME, icon, click);
            return actionsMenu;
        }

        /// <summary>Gets the content of the current.</summary>
        /// <param name="value">The value.</param>
        /// <param name="isFallback">if set to <c>true</c> [is fallback].</param>
        /// <returns></returns>
        [CanBeNull]
        protected Item GetCurrentContent([NotNull] VariableValueItemStub value, out bool isFallback)
        {
            Assert.ArgumentNotNull(value, "value");

            isFallback = false;
            Item contentItem = null;
            if (!string.IsNullOrEmpty(value.Datasource))
            {
                return Client.ContentDatabase.GetItem(value.Datasource);
            }

            if (Rendering != null && !string.IsNullOrEmpty(Rendering.Datasource))
            {
                contentItem = Client.ContentDatabase.GetItem(Rendering.Datasource);
                isFallback = true;
            }

            return contentItem;
        }

        /// <summary>Renders the content controls.</summary>
        /// <param name="output">The output.</param>
        /// <param name="value">The value.</param>
        protected void RenderContentControls([NotNull] HtmlTextWriter output, [NotNull] VariableValueItemStub value)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(value, "value");

            var id = value.Id.ToShortID();
            Item contentItem = null;
            bool isDefault;
            contentItem = GetCurrentContent(value, out isDefault);

            var cssClass = isDefault ? "default-values" : string.Empty;
            if (value.HideComponent)
            {
                cssClass += " display-off";
            }

            output.Write("<div {0} id='{1}_content'>", cssClass == string.Empty ? cssClass : ("class='" + cssClass + "'"), id);
            var clickCommand = value.HideComponent ? "javascript:void(0);" : "variation:setcontent(variationid=" + id + ")";
            var resetCommand = value.HideComponent
                                    ? "javascript:void(0);"
                                    : "ResetVariationContent(\\\"{0}\\\")".FormatWith(id);

            if (contentItem == null)
            {
                RenderPicker(output, value.Datasource, clickCommand, resetCommand, true);
            }
            else
            {
                RenderPicker(output, contentItem, clickCommand, resetCommand, true);
            }
            //RenderContentSearch(output, id, null);
            output.Write("</div>");
        }

        /// <summary>
        /// Selects the search result.
        /// </summary>
        /// <param name="resultid">The resultid.</param>
        /// <param name="variationId">The variation identifier.</param>
        protected void SelectSearchResult(string resultid, string variationId)
        {
            var values = VariableValues;
            var value = values.Find(v => v.Id == new ID(variationId));
            if (value == null)
            {
                return;
            }

            value.Datasource = new ID(resultid).ToString();
            var writer = new HtmlTextWriter(new StringWriter());
            RenderContentControls(writer, value);

            SheerResponse.SetOuterHtml(variationId + "_content", writer.InnerWriter.ToString());
            VariableValues = values;
        }

        /// <summary>Renders the display controls.</summary>
        /// <param name="output">The output.</param>
        /// <param name="value">The value.</param>
        protected void RenderDisplayControls([NotNull] HtmlTextWriter output, [NotNull] VariableValueItemStub value)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(value, "value");

            var id = value.Id.ToShortID();
            output.Write("<input type='checkbox' onfocus='blur();' onclick=\"javascript:return scSwitchRendering(this, event, '{0}')\" ", id);
            if (value.HideComponent)
            {
                output.Write(" checked='checked' ");
            }

            output.Write("/>");
            output.Write("<span class='display-component-title'>");
            output.Write(Translate.Text(Sitecore.Texts.HideComponent));
            output.Write("</span>");
        }

        /// <summary>
        /// Renders the picker.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="item">The item.</param>
        /// <param name="click">The click command.</param>
        /// <param name="reset">The reset command.</param>
        /// <param name="prependEllipsis">if set to <c>true</c> [prepend ellipsis].</param>
        protected void RenderPicker(
          [NotNull] HtmlTextWriter writer,
          [CanBeNull] Item item,
          [NotNull] string click,
          [NotNull] string reset,
          bool prependEllipsis)
        {
            Assert.ArgumentNotNull(writer, "writer");
            Assert.ArgumentNotNull(click, "click");
            Assert.ArgumentNotNull(reset, "reset");

            var icon = Images.GetThemedImageSource(
              item != null ? item.Appearance.Icon : string.Empty, ImageDimension.id16x16);

            var name = Translate.Text(Sitecore.Texts.NOT_SET);
            var cssClass = "item-picker";
            if (item != null)
            {
                name = prependEllipsis ? ".../" : string.Empty;
                name += item.DisplayName;
            }
            else
            {
                cssClass += " not-set";
            }

            writer.Write(string.Format("<div style=\"background-image:url('{0}')\" class='{1}'>", icon, cssClass));
            writer.Write(
              "<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>",
              Context.ClientPage.GetClientEvent(click),
              Translate.Text(Sitecore.Texts.SELECT));

            writer.Write(
              "<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>",
              Context.ClientPage.GetClientEvent(reset),
              Translate.Text(Sitecore.Texts.RESET));

            writer.Write("<span title=\"{0}\">{1}</span>", item == null ? string.Empty : item.DisplayName, name);
            writer.Write("</div>");
        }

        protected void RenderPicker([NotNull] HtmlTextWriter writer, [CanBeNull] string datasource, [NotNull] string clickCommand, [NotNull] string resetCommand, bool prependEllipsis)
        {
            Assert.ArgumentNotNull(writer, "writer");
            Assert.ArgumentNotNull(clickCommand, "clickCommand");
            Assert.ArgumentNotNull(resetCommand, "resetCommand");

            var click = clickCommand;
            var reset = resetCommand;
            var name = Translate.Text(Sitecore.Texts.NOT_SET);
            var cssClass = "item-picker";
            if (!datasource.IsNullOrEmpty())
            {
                name = datasource;
            }
            else
            {
                cssClass += " not-set";
            }

            writer.Write(string.Format("<div class='{0}'>", cssClass));
            writer.Write(
              "<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>",
              Context.ClientPage.GetClientEvent(click),
              Translate.Text(Sitecore.Texts.SELECT));

            writer.Write(
              "<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>",
              Context.ClientPage.GetClientEvent(reset),
              Translate.Text(Sitecore.Texts.RESET));
            var displayName = name;
            if (displayName != null)
            {
                if (displayName.Length > 30)
                {
                    displayName = displayName.Substring(0, 29) + "...";
                }
            }

            writer.Write("<span title=\"{0}\">{1}</span>", name, displayName);
            writer.Write("</div>");
        }

        /// <summary>Renders the variable value.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The variable value.</returns>
        [NotNull]
        protected string RenderVariableValue([NotNull] VariableValueItemStub value)
        {
            var renderer = new CollapsiblePanelRenderer();
            var actionContext = new CollapsiblePanelRenderer.ActionsContext { IsVisible = true };
            var id = value.Id.ToShortID().ToString();
            actionContext.Menu = GetActionsMenu(id);
            var nameContext = new CollapsiblePanelRenderer.NameContext(value.Name)
            {
                OnNameChanged = "javascript:Sitecore.CollapsiblePanel.nameChanged(this, event)"
            };

            var panel = RenderVariableValueDetails(value);
            return renderer.Render(id, panel, nameContext, actionContext);
        }

        /// <summary>The render variable value details.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The render variable value details.</returns>
        protected string RenderVariableValueDetails([NotNull] VariableValueItemStub value)
        {
            var output = new HtmlTextWriter(new StringWriter());
            output.Write("<table class='top-row'>");
            output.Write("<tr>");
            output.Write("<td class='left test-title'>");

            output.Write(Translate.Text(Sitecore.Texts.TestContent));
            output.Write("</td>");
            output.Write("<td class='right'>");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td class='left test-content'>");
            RenderContentControls(output, value);
            output.Write("</td>");
            output.Write("<td class='right display-component'>");
            RenderDisplayControls(output, value);
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr class='component-row'>");
            output.Write("<td class='left test-title'>");
            output.Write(Translate.Text(Sitecore.Texts.TestComponent));
            output.Write("</td>");
            output.Write("<td rowspan='2' class='right'>");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr class='component-row'>");
            output.Write("<td class='left test-component'>");
            RenderComponentControls(output, value);
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("</table>");

            return output.InnerWriter.ToString();
        }

        /// <summary>
        /// Renders the component controls.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="value">The value.</param>
        protected void RenderComponentControls([NotNull] HtmlTextWriter output, [NotNull] VariableValueItemStub value)
        {
            var id = value.Id.ToShortID();
            bool isDefault;
            var renderingItem = GetCurrentRenderingItem(value, out isDefault);
            var src = GetThumbnailSrc(renderingItem);
            var cssClass = isDefault ? "default-values" : string.Empty;
            if (value.HideComponent)
            {
                cssClass += " display-off";
            }

            output.Write(
              "<div id='{0}_component' {1}>", id, string.IsNullOrEmpty(cssClass) ? string.Empty : ("class='" + cssClass + "'"));
            output.Write("<div style=\"background-image:url('{0}')\" class='thumbnail-container'>", src);
            output.Write("</div>");
            output.Write("<div class='picker-container'>");

            var clickCommand = value.HideComponent ? "javascript:void(0);" : "variation:setcomponent(variationid=" + id + ")";
            var resetCommand = value.HideComponent
                                    ? "javascript:void(0);"
                                    : "ResetVariationComponent(\\\"{0}\\\")".FormatWith(id);

            RenderPicker(output, renderingItem, clickCommand, resetCommand, false);
            output.Write("</div>");
            output.Write("</div>");
        }

        [NotNull]
        protected static string GetThumbnailSrc([CanBeNull] Item item)
        {
            var src = "/sitecore/shell/blank.gif";
            if (item == null)
            {
                return src;
            }

            if (!string.IsNullOrEmpty(item.Appearance.Thumbnail) && item.Appearance.Thumbnail != Sitecore.Configuration.Settings.DefaultThumbnail)
            {
                const int thumbnailHeight = 128;
                const int thumbnailWidth = 128;
                var thumbnailSrc = UIUtil.GetThumbnailSrc(item, thumbnailHeight, thumbnailWidth);

                if (!string.IsNullOrEmpty(thumbnailSrc))
                {
                    src = thumbnailSrc;
                }
            }
            else
            {
                src = Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id48x48);
            }

            return src;
        }

        /// <summary>
        /// Gets the current rendering item.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="isFallback">if set to <c>true</c> [is fallback].</param>
        /// <returns>The current rendering item.</returns>
        [CanBeNull]
        protected Item GetCurrentRenderingItem([NotNull] VariableValueItemStub value, out bool isFallback)
        {
            isFallback = false;
            if (!string.IsNullOrEmpty(value.ReplacementComponent))
            {
                return Client.ContentDatabase.GetItem(value.ReplacementComponent);
            }

            var curRendering = Rendering;
            if (curRendering == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(curRendering.ItemID))
            {
                isFallback = true;
                return Client.ContentDatabase.GetItem(curRendering.ItemID);
            }

            return null;
        }

        /// <summary>
        /// Sets the state of the controls.
        /// </summary>
        protected void SetControlsState()
        {
            var count = VariableValues.Count;
            OK.Disabled = count < 2;
            NoVariations.Visible = count < 1;
            NewVariation.Disabled = count > TestValueCollection.MaxVariableValues;
        }

        #endregion

        /// <summary>The variable value item stub.</summary>
        [Serializable]
        protected class VariableValueItemStub
        {
            #region Constants and Fields

            /// <summary>The datasource.</summary>
            public string Datasource;

            /// <summary>The component hidden.</summary>
            public bool HideComponent;

            /// <summary>The name.</summary>
            public string Name;

            /// <summary>The replacement component.</summary>
            public string ReplacementComponent;

            /// <summary>Determines if this is the original value.</summary>
            public bool IsOriginal;

            /// <summary>The id.</summary>
            private readonly string id;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="VariableValueItemStub"/> class.
            /// </summary>
            /// <param name="id">The id.</param>
            /// <param name="name">The name.</param>
            public VariableValueItemStub([NotNull] ID id, [NotNull] string name)
            {
                Assert.ArgumentNotNull(id, "id");
                Assert.ArgumentNotNull(name, "name");

                Datasource = string.Empty;
                HideComponent = false;
                ReplacementComponent = string.Empty;
                Name = name;
                IsOriginal = false;
                this.id = id.ToShortID().ToString();
            }

            #endregion

            #region Properties

            /// <summary>Gets Id.</summary>
            public ID Id
            {
                get
                {
                    return string.IsNullOrEmpty(id) ? ID.Null : ShortID.DecodeID(id);
                }
            }

            #endregion
        }
    }
}