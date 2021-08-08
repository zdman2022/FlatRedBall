﻿using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using OfficialPlugins.Compiler.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;
using System.Windows;
using OfficialPlugins.Compiler.CodeGeneration;
using System.Net.Sockets;
using OfficialPlugins.Compiler.Managers;
using FlatRedBall.Glue.Controls;
using System.ComponentModel;
using FlatRedBall.Glue.IO;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.Models;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPluginsCore.Compiler.ViewModels;
using OfficialPluginsCore.Compiler.Managers;
using System.Diagnostics;
using System.Timers;
using Glue;
using OfficialPluginsCore.Compiler.CommandReceiving;
using FlatRedBall.Glue.Elements;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.CommandSending;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        MainControl control;

        Compiler compiler;
        Runner runner;
        CompilerViewModel viewModel;

        public static CompilerViewModel MainViewModel { get; private set; }

        PluginTab buildTab;

        Game1GlueControlGenerator game1GlueControlGenerator;

        public override string FriendlyName => "Glue Compiler";

        public override Version Version
        {
            get
            {
                // 0.4 introduces:
                // - multicore building
                // - Removed warnings and information when building - now we just show start, end, and errors
                // - If an error occurs, a popup appears telling the user that the game crashed, and to open Visual Studio
                // 0.5
                // - Support for running content-only builds
                // 0.6
                // - Added VS 2017 support
                // 0.7
                // - Added a list of MSBuild locations
                return new Version(0, 7);
            }
        }

        FilePath JsonSettingsFilePath => GlueState.Self.ProjectSpecificSettingsFolder + "CompilerSettings.json";

        bool ignoreViewModelChanges = false;

        Timer timer;

        #endregion

        public override void StartUp()
        {
            CreateControl();

            CreateToolbar();

            RefreshManager.Self.InitializeEvents(this.control.PrintOutput, this.control.PrintOutput);

            Output.Initialize(this.control.PrintOutput);

            AssignEvents();

            compiler = Compiler.Self;
            runner = Runner.Self;

            game1GlueControlGenerator = new Game1GlueControlGenerator();
            this.RegisterCodeGenerator(game1GlueControlGenerator);

            this.RegisterCodeGenerator(new CompilerPluginElementCodeGenerator());

            #region Start the timer

            var timerFrequency = 400; // ms
            timer = new Timer(timerFrequency);
            timer.Elapsed += HandleTimerElapsed;
            timer.SynchronizingObject = MainGlueWindow.Self;
            timer.Start();

            #endregion
        }

        private async void HandleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if(viewModel.IsEditChecked)
                {
                    var gameToGlueCommandsAsString = await CommandSending.CommandSender
                        .SendCommand("GetCommands", viewModel.PortNumber);

                    if (!string.IsNullOrEmpty(gameToGlueCommandsAsString))
                    {
                        CommandReceiver.HandleCommandsFromGame(gameToGlueCommandsAsString, viewModel.PortNumber);
                    }
                }

            }
            catch
            {
                // it's okay
            }
        }

        private void AssignEvents()
        {
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
            this.ReactToNewFileHandler += RefreshManager.Self.HandleNewFile;

            this.ReactToFileChangeHandler += (fileName) =>
                RefreshManager.Self.HandleFileChanged(new FlatRedBall.IO.FilePath(fileName));
            this.ReactToCodeFileChange += RefreshManager.Self.HandleFileChanged;
            this.NewEntityCreated += RefreshManager.Self.HandleNewEntityCreated;


            this.NewScreenCreated += (newScreen) =>
            {
                ToolbarController.Self.HandleNewScreenCreated(newScreen);
                RefreshManager.Self.HandleNewScreenCreated();
            };
            this.ReactToScreenRemoved += ToolbarController.Self.HandleScreenRemoved;
            // todo - handle startup changed...
            this.ReactToNewObjectHandler += RefreshManager.Self.HandleNewObjectCreated;
            this.ReactToObjectRemoved += async (owner, nos) =>
                await RefreshManager.Self.HandleObjectRemoved(owner, nos);
            this.ReactToElementVariableChange += RefreshManager.Self.HandleVariableChanged;
            this.ReactToNamedObjectChangedValue += (string changedMember, object oldValue, NamedObjectSave namedObject) => 
                RefreshManager.Self.HandleNamedObjectValueChanged(changedMember, oldValue, namedObject, Dtos.AssignOrRecordOnly.Assign);
            this.ReactToChangedStartupScreen += ToolbarController.Self.ReactToChangedStartupScreen;
            this.ReactToItemSelectHandler += RefreshManager.Self.HandleItemSelected;
            this.ReactToObjectContainerChanged += RefreshManager.Self.HandleObjectContainerChanged;
            // If a variable is added, that may be used later to control initialization.
            // The game won't reflect that until it has been restarted, so let's just take 
            // care of it now. For variable removal I don't know if any restart is needed...
            this.ReactToVariableAdded += RefreshManager.Self.HandleVariableAdded;
            this.ReactToStateCreated += RefreshManager.Self.HandleStateCreated;
            this.ReactToStateVariableChanged += RefreshManager.Self.HandleStateVariableChanged;
        }

        private void HandleGluxUnloaded()
        {
            viewModel.CompileContentButtonVisibility = Visibility.Collapsed;
            viewModel.HasLoadedGlux = false;

            ToolbarController.Self.HandleGluxUnloaded();
        }

        private CompilerSettingsModel LoadOrCreateCompilerSettings()
        {
            CompilerSettingsModel compilerSettings = new CompilerSettingsModel();
            var filePath = JsonSettingsFilePath;
            if (filePath.Exists())
            {
                try
                {
                    var text = System.IO.File.ReadAllText(filePath.FullPath);
                    compilerSettings = JsonConvert.DeserializeObject<CompilerSettingsModel>(text);
                }
                catch
                {
                    // do nothing, it'll just get wiped out and re-saved later
                }
            }

            return compilerSettings;
        }

        private bool IsFrbNewEnough()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if(mainProject.IsFrbSourceLinked())
            {
                return true;
            }
            else
            {
                return GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            }
        }

        private void HandleGluxLoaded()
        {
            UpdateCompileContentVisibility();

            var model = LoadOrCreateCompilerSettings();
            ignoreViewModelChanges = true;
            viewModel.SetFrom(model);
            ignoreViewModelChanges = false;

            viewModel.IsGluxVersionNewEnoughForGlueControlGeneration =
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1;
            viewModel.HasLoadedGlux = true;

            game1GlueControlGenerator.PortNumber = model.PortNumber;
            game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = model.GenerateGlueControlManagerCode;
            RefreshManager.Self.PortNumber = model.PortNumber;

            ToolbarController.Self.HandleGluxLoaded();

            if(IsFrbNewEnough())
            {
                TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(model.GenerateGlueControlManagerCode), "Generate Glue Control Code");
            }

            GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");
        }

        private void UpdateCompileContentVisibility()
        {
            bool shouldShowCompileContentButton = false;

            if (GlueState.Self.CurrentMainProject != null)
            {
                shouldShowCompileContentButton = GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject;

                if (!shouldShowCompileContentButton)
                {
                    foreach (var mainSyncedProject in GlueState.Self.SyncedProjects)
                    {
                        if (mainSyncedProject != mainSyncedProject.ContentProject)
                        {
                            shouldShowCompileContentButton = true;
                            break;
                        }
                    }
                }

            }

            if (shouldShowCompileContentButton)
            {
                viewModel.CompileContentButtonVisibility = Visibility.Visible;
            }
            else
            {
                viewModel.CompileContentButtonVisibility = Visibility.Collapsed;
            }
        }

        private void CreateToolbar()
        {
            var toolbar = new RunnerToolbar();
            toolbar.RunClicked += HandleToolbarRunClicked;

            ToolbarController.Self.Initialize(toolbar);

            toolbar.DataContext = ToolbarController.Self.GetViewModel();

            base.AddToToolBar(toolbar, "Standard");
        }

        private void HandleFileChanged(string fileName)
        {
            bool shouldBuildContent = viewModel.AutoBuildContent &&
                GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject &&
                GlueState.Self.CurrentMainContentProject.IsFilePartOfProject(fileName);

            if (shouldBuildContent)
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Building for changed file {fileName}");

                BuildContent(OutputSuccessOrFailure);
            }

        }

        private async void HandleToolbarRunClicked(object sender, EventArgs e)
        {
            await BuildAndRun();
        }

        public async Task BuildAndRun()
        {
            if (viewModel.IsToolbarPlayButtonEnabled)
            {
                GlueCommands.Self.DialogCommands.FocusTab("Build");
                var succeeded = await Compile();

                if (succeeded)
                {
                    bool hasErrors = GetIfHasErrors();
                    if (hasErrors)
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () => await runner.Run(preventFocus: false));
                    }
                    else
                    {
                        PluginManager.ReceiveOutput("Building succeeded. Running project...");

                        await runner.Run(preventFocus: false);
                    }
                }
                else
                {
                    PluginManager.ReceiveError("Building failed. See \"Build\" tab for more information.");
                }
            }
        }

        private void CreateControl()
        {
            viewModel = new CompilerViewModel();
            viewModel.Configuration = "Debug";
            viewModel.IsRebuildAndRestartEnabled = true;

            viewModel.PropertyChanged += HandleMainViewModelPropertyChanged;

            MainViewModel = viewModel;

            control = new MainControl();
            control.DataContext = viewModel;

            Runner.Self.ViewModel = viewModel;
            RefreshManager.Self.ViewModel = viewModel;
            VariableSendingManager.Self.ViewModel = viewModel;

            buildTab = base.CreateTab(control, "Build");
            buildTab.SuggestedLocation = TabLocation.Bottom;
            buildTab.Show();

            AssignControlEvents();
        }

        private async void HandleMainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out////////////////
            var propertyName = e.PropertyName;

            switch (propertyName)
            {
                case nameof(CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked):
                case nameof(CompilerViewModel.PortNumber):
                    control.PrintOutput("Applying changes");
                    game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = viewModel.IsGenerateGlueControlManagerInGame1Checked;
                    game1GlueControlGenerator.PortNumber = viewModel.PortNumber;
                    RefreshManager.Self.PortNumber = viewModel.PortNumber;
                    GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
                    var model = viewModel.ToModel();
                    try
                    {
                        var text = JsonConvert.SerializeObject(model);
                        GlueCommands.Self.TryMultipleTimes(() =>
                        {
                            System.IO.Directory.CreateDirectory(JsonSettingsFilePath.GetDirectoryContainingThis().FullPath);
                            System.IO.File.WriteAllText(JsonSettingsFilePath.FullPath, text);
                        });
                    }
                    catch
                    {
                        // no big deal if it fails
                    }
                    if (IsFrbNewEnough())
                    {
                        TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(model.GenerateGlueControlManagerCode), "Generate Glue Control Code");
                    }

                    if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.NugetPackageInCsproj)
                    {
                        GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");
                    }

                    RefreshManager.Self.StopAndRestartTask($"{propertyName} changed");

                    break;
                case nameof(CompilerViewModel.CurrentGameSpeed):
                    var speedPercentage = int.Parse(viewModel.CurrentGameSpeed.Substring(0, viewModel.CurrentGameSpeed.Length - 1));
                    await CommandSender.Send(new SetSpeedDto
                    {
                        SpeedPercentage = speedPercentage
                    }, viewModel.PortNumber);
                    
                    break;
                case nameof(CompilerViewModel.EffectiveIsRebuildAndRestartEnabled):
                    RefreshManager.Self.IsExplicitlySetRebuildAndRestartEnabled = viewModel.EffectiveIsRebuildAndRestartEnabled;
                    break;
                case nameof(CompilerViewModel.IsToolbarPlayButtonEnabled):
                    ToolbarController.Self.SetEnabled(viewModel.IsToolbarPlayButtonEnabled);
                    break;
                case nameof(CompilerViewModel.PlayOrEdit):

                    var inEditMode = viewModel.PlayOrEdit == PlayOrEdit.Edit;
                    await CommandSending.CommandSender.Send(
                        new Dtos.SetEditMode { IsInEditMode = inEditMode },
                        viewModel.PortNumber);

                    if (inEditMode)
                    {
                        var currentEntity = GlueCommands.Self.DoOnUiThread<EntitySave>(() => GlueState.Self.CurrentEntitySave);
                        if(currentEntity != null)
                        {
                            await GlueCommands.Self.DoOnUiThread(async () => await RefreshManager.Self.PushGlueSelectionToGame());
                        }
                        else
                        {
                            var screenName = await CommandSending.CommandSender.GetScreenName(viewModel.PortNumber);

                            if (!string.IsNullOrEmpty(screenName))
                            {
                                var glueScreenName =
                                    string.Join('\\', screenName.Split('.').Skip(1).ToArray());

                                var screen = ObjectFinder.Self.GetScreenSave(glueScreenName);

                                if (screen != null)
                                {
                                    GlueCommands.Self.DoOnUiThread(() =>
                                    {
                                        if(GlueState.Self.CurrentElement != screen)
                                        {
                                            GlueState.Self.CurrentElement = screen;
                                        }
                                    });
                                }
                            }
                        }
                    }

                    break;
            }
        }

        private void AssignControlEvents()
        {
            control.BuildClicked += async (not, used) =>
            {
                await Compile();
            };

            control.StopClicked += (not, used) =>
            {
                runner.KillGameProcess();
            };

            control.RestartGameClicked += async (not, used) =>
            {
                viewModel.IsPaused = false;
                runner.KillGameProcess();
                var succeeded = await Compile();
                if (succeeded)
                {
                    await runner.Run(preventFocus: false);
                }
            };

            control.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                var wasEditChecked = viewModel.IsEditChecked;
                var screenName = await CommandSending.CommandSender.GetScreenName(viewModel.PortNumber);


                viewModel.IsPaused = false;
                runner.KillGameProcess();
                var succeeded = await Compile();

                if (succeeded)
                {
                    if (succeeded)
                    {
                        await runner.Run(preventFocus: false, screenName);
                        if (wasEditChecked)
                        {
                            viewModel.IsEditChecked = true;
                        }
                    }
                }
            };

            control.RestartScreenClicked += async (not, used) =>
            {
                viewModel.IsPaused = false;
                await CommandSender.Send(new RestartScreenDto(), viewModel.PortNumber);
            };

            control.AdvanceOneFrameClicked += async (not, used) =>
            {
                await CommandSender.Send(new AdvanceOneFrameDto(), viewModel.PortNumber);
            };

            control.BuildContentClicked += delegate
            {
                BuildContent(OutputSuccessOrFailure);
            };

            control.RunClicked += async (not, used) =>
            {
                var succeeded = await Compile();
                if (succeeded)
                {
                    if (succeeded)
                    {
                        await runner.Run(preventFocus: false);
                    }
                    else
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () => await runner.Run(preventFocus: false));
                    }
                }
            };

            control.PauseClicked += async (not, used) =>
            {
                viewModel.IsPaused = true;
                await CommandSender.Send(new TogglePauseDto(), viewModel.PortNumber);
            };

            control.UnpauseClicked += async (not, used) =>
            {
                viewModel.IsPaused = false;
                await CommandSender.Send(new TogglePauseDto(), viewModel.PortNumber);
            };
        }

        private static bool GetIfHasErrors()
        {
            var errorPlugin = PluginManager.AllPluginContainers
                                .FirstOrDefault(item => item.Plugin is ErrorPlugin.MainErrorPlugin)?.Plugin as ErrorPlugin.MainErrorPlugin;

            var hasErrors = errorPlugin?.HasErrors == true;
            return hasErrors;
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

        private void BuildContent(Action<bool> afterCompile = null)
        {
            compiler.BuildContent(control.PrintOutput, control.PrintOutput, afterCompile, viewModel.Configuration);
        }

        private async Task<bool> Compile()
        {
            viewModel.IsCompiling = true;
            var toReturn = await compiler.Compile(
                control.PrintOutput,
                control.PrintOutput,
                viewModel.Configuration);
            viewModel.IsCompiling = false;
            return toReturn;
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public bool GetIfIsRunningInEditMode()
        {
            return viewModel.IsEditChecked && viewModel.IsRunning;
        }
    }

}
