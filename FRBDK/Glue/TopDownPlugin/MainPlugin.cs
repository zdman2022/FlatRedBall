﻿using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.CodeGenerators;
using TopDownPlugin.Controllers;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;

namespace TopDownPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Top Down Plugin";

        // 1.1 - Added support for 0 time speedup and slowdown
        // 1.2 - Fixed direction being reset when not moving with 
        //       a slowdown time of 0.
        // 1.3 - Added ability to get direction from velocity
        //     - Added ability to invert direction and mirror direction
        // 1.4 - Added InitializeTopDownInput which takes an IInputDevice
        // 1.4.1 - Added TopDownDirection.ToString
        // 1.5 - Added TopDownAiInput.IsActive which can disabled AI input if set to false
        // 1.6 - InitializeTopDownInput now calls a partial method allowing custom code to
        //       add its own logic.
        // 1.7 - Added TopDownSpeedMultiplier allowing speed to be multiplied easily based on terrain or power-ups
        // 1.7.1 - Will ask the user if plugin should be a required plugin when marking an entity as top-down
        // 2.0 - New UI for editing top down values
        //  - TopDownAiInput.cs is now saved in TopDownAiInput.Generated.cs
        public override Version Version => 
            new Version(2, 0, 0);

        MainEntityView control;

        PluginTab pluginTab;

        #endregion

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            MainController.Self.MainPlugin = this;

            base.RegisterCodeGenerator(new EntityCodeGenerator());
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToEntityRemoved += HandleElementRemoved;
        }

        private void HandleGluxLoaded()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;

            var anyTopDownEntities = entities.Any(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown));
            });

            if (anyTopDownEntities)
            {
                // just in case it's not there:
                EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                InterfacesFileGenerator.Self.GenerateAndSave();
                AiCodeGenerator.Self.GenerateAndSave();

            }
        }

        private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


            if (shouldShow)
            {
                if (control == null)
                {
                    control = MainController.Self.GetControl();
                    pluginTab = this.CreateTab(control, "Top Down");
                    this.ShowTab(pluginTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(pluginTab);
                }
                MainController.Self.UpdateTo(GlueState.Self.CurrentEntitySave);
            }
            else
            {
                this.RemoveTab(pluginTab);
            }
        }

        private void HandleElementRemoved(EntitySave removedElement, List<string> additionalFiles)
        {
            // This could be the very last entity that was a top-down, but isn't
            // anymore.
            MainController.Self.CheckForNoTopDownEntities();
        }
    }
}
