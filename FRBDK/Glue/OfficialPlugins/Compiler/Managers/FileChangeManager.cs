﻿using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.Compiler.Managers
{


    class FileChangeManager 
    {
        string[] copiedExtensions = new[]
        {
            "csv",
            "txt",
            "png",
            "tmx",
            "tsx",
            "bmp",
            "png",
            "achx",
            "emix",
            "json"
        };

        MainControl control;
        Compiler compiler;
        CompilerViewModel viewModel;

        public FileChangeManager(MainControl control, Compiler compiler, CompilerViewModel viewModel)
        {
            this.control = control;
            this.compiler = compiler;
            this.viewModel = viewModel;
        }

        public void HandleFileChanged(string fileName)
        {
            // If a file changed, always copy it over - why only do so if we're in edit mode?

            bool shouldBuildContent = viewModel.AutoBuildContent &&
                GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject &&
                GlueState.Self.CurrentMainContentProject.IsFilePartOfProject(fileName);

            if (shouldBuildContent)
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Building for changed file {fileName}");

                BuildContent(OutputSuccessOrFailure);
            }

            var extension = FileManager.GetExtension(fileName);
            var shouldCopy = copiedExtensions.Contains(extension);

            if (shouldCopy)
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(fileName);

            }

            RefreshManager.Self.HandleFileChanged(fileName);
        }


        private void BuildContent(Action<bool> afterCompile = null)
        {
            compiler.BuildContent(control.PrintOutput, control.PrintOutput, afterCompile, viewModel.Configuration);
        }

        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build failed");

            }
        }
    }
}