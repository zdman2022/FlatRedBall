﻿using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using OfficialPlugins.Common.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using WpfDataUi.Controls;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.SpritePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainSpritePlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Sprite Plugin";

        public override Version Version => new Version(1,0);

        #endregion

        public override void StartUp()
        {
            ModifySpriteAti();

            // This should be early so the variable can be added before codegen:
            this.ReactToLoadedGluxEarly += HandleGluxLoaded;
        }

        private void HandleGluxLoaded()
        {
            var shouldHaveUseAnimationTextureFlip = 
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasUseAnimationTextureFlip;

            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var existingUseAnimationTextureVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.UseAnimationTextureFlip));
            var existingIgnoreAnimationTextureFlipVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip));

            var doesAtiAlreadyHaveUseAnimationTextureFlip = existingUseAnimationTextureVariableDefinition != null;

            // Update the presence of the UseAnimationTextureFlip variable definition
            if(shouldHaveUseAnimationTextureFlip && !doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                var useAnimationTextureFlipVariableDefinition = new VariableDefinition();
                useAnimationTextureFlipVariableDefinition.Type = "bool";
                useAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.UseAnimationTextureFlip);
                useAnimationTextureFlipVariableDefinition.Category = "Animation";
                useAnimationTextureFlipVariableDefinition.DefaultValue = "true";

                var useAnimationRelativePositionVariableDefinition = ati.VariableDefinitions.FirstOrDefault(item =>
                    item.Name == nameof(FlatRedBall.Sprite.UseAnimationRelativePosition));
                if(useAnimationRelativePositionVariableDefinition != null)
                {
                    var indexOf = ati.VariableDefinitions.IndexOf(useAnimationRelativePositionVariableDefinition);

                    ati.VariableDefinitions.Insert(indexOf + 1, useAnimationTextureFlipVariableDefinition);

                }
                else
                {
                    ati.VariableDefinitions.Add(useAnimationTextureFlipVariableDefinition);
                }
            }
            else if(!shouldHaveUseAnimationTextureFlip && doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                ati.VariableDefinitions.Remove(existingUseAnimationTextureVariableDefinition);
            }

            if(shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition != null)
            {
                ati.VariableDefinitions.Remove(existingIgnoreAnimationTextureFlipVariableDefinition);
            }
            if(!shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition == null)
            {
                var ignoreAnimationTextureFlipVariableDefinition = new VariableDefinition();
                ignoreAnimationTextureFlipVariableDefinition.Type = "bool";
                ignoreAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip);
                ignoreAnimationTextureFlipVariableDefinition.Category = "Animation";
                ignoreAnimationTextureFlipVariableDefinition.DefaultValue = "false";

                ati.VariableDefinitions.Add(ignoreAnimationTextureFlipVariableDefinition);
            }
        }

        private static void ModifySpriteAti()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var textureVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Texture");
            if (textureVariable != null)
            {
                textureVariable.PreferredDisplayer = typeof(EditableComboBoxDisplay);
            }

            //var colorVariableDefinition = new VariableDefinition();
            //colorVariableDefinition.PreferredDisplayer = typeof (ColorDisplay);
            //colorVariableDefinition.Name = "Color";
            //colorVariableDefinition.Type = "Color";
            //colorVariableDefinition.DefaultValue = null;
            //colorVariableDefinition.Category = "Appearance";
            //ati.VariableDefinitions.Add(colorVariableDefinition);

            var redVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Red");
            redVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;
            

            var greenVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Green");
            greenVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Blue");
            blueVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueIndex = ati.VariableDefinitions.IndexOf(blueVariableDefinition);

            var colorHexValueDefinition = new VariableDefinition();
            colorHexValueDefinition.Name = "Color Hex";
            colorHexValueDefinition.Category = "Appearance";
            colorHexValueDefinition.DefaultValue = null;
            colorHexValueDefinition.Type = "string";
            colorHexValueDefinition.UsesCustomCodeGeneration = true;
            colorHexValueDefinition.CustomVariableGet = (element, nos) =>
            {
                var red = ((ObjectFinder.GetValueRecursively(nos, element, "Red") as float?) ?? 0) * 255;
                var green = ((ObjectFinder.GetValueRecursively(nos, element, "Green") as float?) ?? 0) * 255;
                var blue = ((ObjectFinder.GetValueRecursively(nos, element, "Blue") as float?) ?? 0) * 255;

                var redInt = MathFunctions.RoundToInt(red);
                var greenInt = MathFunctions.RoundToInt(green);
                var blueInt = MathFunctions.RoundToInt(blue);

                // source: https://stackoverflow.com/questions/39137486/converting-colour-name-to-hex-in-c-sharp
                var hexValue = $"{redInt:X2}{greenInt:X2}{blueInt:X2}";
                return hexValue;
            };
            colorHexValueDefinition.CustomVariableSet = (element, nos, newValue) =>
            {
                var colorConverter = new ColorConverter();
                var newValueAsString = newValue as string;
                if(!string.IsNullOrEmpty(newValueAsString))
                {
                    if(!newValueAsString.StartsWith("#"))
                    {
                        newValueAsString = "#" + newValueAsString;
                    }
                    try
                    {
                        var color = (Color)colorConverter.ConvertFromString(newValueAsString);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, "Red", color.R / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, "Green", color.G / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, "Blue", color.B / 255.0f, performSaveAndGenerateCode: true, updateUi: true);
                    }
                    catch
                    {
                        // do we want to do anything?
                    }

                }
            };
            ati.VariableDefinitions.Insert(blueIndex + 1, colorHexValueDefinition);

        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
