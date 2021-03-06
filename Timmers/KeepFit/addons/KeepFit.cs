﻿using KSP.IO;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KeepFit
{
    /*
     * This gets created when the game loads the Space Center scene. It then checks to make sure
     * the scenarios have been added to the game (so they will be automatically created in the
     * appropriate scenes).
     */
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AddScenarioModules : MonoBehaviour
    {
        void Start()
        {
            var game = HighLogic.CurrentGame;

            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(KeepFitScenarioModule).Name);
            if (psm == null)
            {
                this.Log_DebugOnly("Start", "Adding the scenario module.");
                psm = game.AddProtoScenarioModule(typeof(KeepFitScenarioModule), 
                    GameScenes.SPACECENTER,
                    GameScenes.TRACKSTATION, 
                    GameScenes.FLIGHT, 
                    GameScenes.EDITOR);
            }
            else
            {
                this.Log_DebugOnly("Start", "Scenario module already exists.");
                if (!psm.targetScenes.Any(s => s == GameScenes.SPACECENTER))
                {
                    this.Log_DebugOnly("Start", "Adding target scene SPACECENTER to scenario module target scenes.");
                    psm.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (!psm.targetScenes.Any(s => s == GameScenes.TRACKSTATION))
                {
                    this.Log_DebugOnly("Start", "Adding target scene TRACKSTATION to scenario module target scenes.");
                    psm.targetScenes.Add(GameScenes.TRACKSTATION);
                }
                if (!psm.targetScenes.Any(s => s == GameScenes.FLIGHT))
                {
                    this.Log_DebugOnly("Start", "Adding target scene FLIGHT to scenario module target scenes.");
                    psm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (!psm.targetScenes.Any(s => s == GameScenes.EDITOR))
                {
                    this.Log_DebugOnly("Start", "Adding target scene EDITOR to scenario module target scenes.");
                    psm.targetScenes.Add(GameScenes.EDITOR);
                }
            }
        }
    }

    class KeepFitScenarioModule : ScenarioModule
    {
        private ConnectedLivingSpace.ICLSAddon cls;

        /// <summary>
        /// AppLauncher button
        /// </summary>
        private ApplicationLauncherButton appLauncherButton;
        private IButton toolmodLauncherButton;
        private bool toolmodToggleCurrentlyOn;


        private MainWindow mainWindow;
        private RosterWindow rosterWindow;
        private AllVesselsWindow allVesselsWindow;
        private LogWindow logWindow;
        private ConfigWindow configWindow;

        /// <summary>
        /// UI window for displaying the current crew roster
        /// </summary>


        /// <summary>
        /// UI Window for in flight use for displaying the active vessel crew's fitness level
        /// </summary>




        private KeepFitCrewFitnessController crewFitnessController;
        private KeepFitCrewRosterController crewRosterController;
        private KeepFitGeeEffectsController geeEffectsController;

        /// <summary>
        /// Main copy of the per-game config
        /// </summary>
        private GameConfig gameConfig = new GameConfig();
        
        private KeepFitAPIImplementation keepFitAPIImplementation = KeepFitAPIImplementation.instance();


        public KeepFitScenarioModule()
        {
            this.Log_DebugOnly("Constructor", ".");

            gameConfig = new GameConfig();
            
            keepFitAPIImplementation.setGameConfig(gameConfig);
        }

        public void toolmodButtonToggle()
        {
            if (this.toolmodToggleCurrentlyOn)
                onAppLaunchToggleOff();
            else
                onAppLaunchToggleOn();
            this.toolmodToggleCurrentlyOn = !this.toolmodToggleCurrentlyOn;
        }

        public override void OnAwake()
        {
            this.Log_DebugOnly("OnAwake", "Scene[{0}]", HighLogic.LoadedScene);
            base.OnAwake();

            if (cls == null)
            {
                cls = CLSClient.GetCLS();
            }

            if (mainWindow == null)
            {
                mainWindow = gameObject.AddComponent<MainWindow>();
                mainWindow.Init(this);
            }

            if (logWindow == null)
            {
                logWindow = gameObject.AddComponent<LogWindow>();
                logWindow.Init(this);
            }

            if (configWindow == null)
            {
                configWindow = gameObject.AddComponent<ConfigWindow>();
                configWindow.Init(this);
            }

            if (rosterWindow == null)
            {
                this.Log_DebugOnly("OnAwake", "Constructing rosterWindow");
                rosterWindow = gameObject.AddComponent<RosterWindow>();
                rosterWindow.Init(this);
            }

            if (allVesselsWindow == null)
            {
                this.Log_DebugOnly("OnAwake", "Constructing allVesselsWindow");
                allVesselsWindow = gameObject.AddComponent<AllVesselsWindow>();
                allVesselsWindow.Init(this);
            }




            if (appLauncherButton != null)
            {
                this.Log_DebugOnly("OnAwake", "AppLauncher button already here");
            }
            else
            {
                this.Log_DebugOnly("OnAwake", "Adding AppLauncher button");

                Texture toolbarButtonTexture = (Texture)GameDatabase.Instance.GetTexture("KeepFit/KeepFit", false);
                ApplicationLauncher.AppScenes scenes = ApplicationLauncher.AppScenes.FLIGHT |
                                                       //ApplicationLauncher.AppScenes.MAPVIEW |
                                                       ApplicationLauncher.AppScenes.SPACECENTER |
                                                       //ApplicationLauncher.AppScenes.SPH |
                                                       //ApplicationLauncher.AppScenes.VAB |
                                                       ApplicationLauncher.AppScenes.TRACKSTATION;

                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(onAppLaunchToggleOn,
                                                               onAppLaunchToggleOff,
                                                               onAppLaunchHoverOn,
                                                               onAppLaunchHoverOff,
                                                               onAppLaunchEnable,
                                                               onAppLaunchDisable,
                                                               scenes,
                                                               toolbarButtonTexture);
                ApplicationLauncher.Instance.AddOnRepositionCallback(onAppLauncherReposition);

                if (ToolbarManager.ToolbarAvailable && ToolbarManager.Instance != null)
                {
                    this.Log_DebugOnly("OnAwake", "Adding Toolbar Button");
                    this.toolmodLauncherButton = ToolbarManager.Instance.add("KeepFit", "MainButton");
                    if (this.toolmodLauncherButton != null)
                    {
                        appLauncherButton.VisibleInScenes = scenes; // TODO
                        this.toolmodLauncherButton.Visibility = new GameScenesVisibility(new GameScenes[] { GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION });
                        this.toolmodLauncherButton.TexturePath = "KeepFit/UIResources/KeepFit";
                        this.toolmodLauncherButton.ToolTip = "KeepFit Toolbar";
                        this.toolmodLauncherButton.OnClick += ((e) => this.toolmodButtonToggle());
                    }
                }
            }


            this.Log_DebugOnly("OnAwake", "Adding KeepFitCrewRosterController");
            crewRosterController = gameObject.AddComponent<KeepFitCrewRosterController>();
            crewRosterController.Init(this);

            if (HighLogic.LoadedScene == GameScenes.FLIGHT ||
                HighLogic.LoadedScene == GameScenes.TRACKSTATION ||
                HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                this.Log_DebugOnly("OnAwake", "Adding KeepFitCrewFitnessController");
                crewFitnessController = gameObject.AddComponent<KeepFitCrewFitnessController>();
                crewFitnessController.Init(this);
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                this.Log_DebugOnly("OnAwake", "Adding KeepFitGeeEffectsController");
                this.geeEffectsController = gameObject.AddComponent<KeepFitGeeEffectsController>();
                this.geeEffectsController.Init(this);
            }
        }

        public GameConfig GetGameConfig()
        {
            return gameConfig;
        }

        public bool isKeepFitEnabled()
        {
            return gameConfig.enabled;
        }

        public ConnectedLivingSpace.ICLSAddon GetCLS()
        {
            return cls;
        }

        public bool isCrewFitnessControllerActive() 
        {
            return (this.crewFitnessController != null);
        }

        public bool isCrewRosterControllerActive()
        {
            return (this.crewRosterController != null);
        }

        public bool isGeeEffectsControllerActive()
        {
            return (this.geeEffectsController != null);
        }

        public void ShowLog()
        {
            this.logWindow.Show();
        }

        public void ShowRoster()
        {
            this.rosterWindow.Show();
        }

        public void ShowVessels()
        {
            this.allVesselsWindow.Show();
        }

        public void ShowSettings()
        {
            this.configWindow.Show();
        }

        public bool isVesselLandedOnExercisableSurface(Vessel vessel)
        {
            return (vessel != null && vessel.LandedOrSplashed && vessel.geeForce > gameConfig.minimumLandedGeeForExcercising);
        }

        void onAppLauncherReposition()
        {
        }

        void onAppLaunchToggleOn() 
        {
            this.Log_DebugOnly("onAppLaunchToggleOn", "ToggleOn called - showing windows");
            
            /*Your code goes in here to toggle display on regardless of hover*/
            showKeepFitWindow();
        }

        void onAppLaunchToggleOff() 
        {
            this.Log_DebugOnly("onAppLaunchToggleOff", "ToggleOff called - hiding windows");

            /*Your code goes in here to toggle display off regardless of hover*/
            hideKeepFitMainWindow();
        }

        void onAppLaunchHoverOn() 
        {
            this.Log_DebugOnly("onAppLaunchHoverOn", "HoverOn called - does nothing");

            /*Your code goes in here to show display on*/
            //showKeepFitWindow();
        }

        void onAppLaunchHoverOff() 
        {
            this.Log_DebugOnly("onAppLaunchHoverOff", "HoverOff called - does nothing");

            /*Your code goes in here to show display off*/
            //hideKeepFitMainWindow();
        }
        
        void onAppLaunchEnable() 
        {
            this.Log_DebugOnly("onAppLaunchEnable", "LaunchEnable called - ignoring call");//showing window for scene");

            // showKeepFitWindow();
        }

        private void showKeepFitWindow()
        {
            /*Your code goes in here for if it gets enabled*/
            mainWindow.Show();
        }

        void onAppLaunchDisable() 
        {
            this.Log_DebugOnly("onAppLaunchDisable", "LaunchDisable called - ignoring call");//hiding windows");

            // hideKeepFitMainWindow();
        }

        private void hideKeepFitMainWindow()
        {
            this.mainWindow.Hide();
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);

            gameConfig.Load(gameNode, true);
            this.Log_DebugOnly("OnLoad: ", "Loaded gameConfig");

            mainWindow.Load(gameConfig);
            this.Log_DebugOnly("OnLoad: ", "Loaded mainWindow");

            logWindow.Load(gameConfig);
            this.Log_DebugOnly("OnLoad: ", "Loaded logWindow");

            configWindow.Load(gameConfig);
            this.Log_DebugOnly("OnLoad: ", "Loaded configWindow");

            rosterWindow.Load(gameConfig);
            this.Log_DebugOnly("OnLoad: ", "Loaded rosterWindow");
        }

        public override void OnSave(ConfigNode gameNode)
        {
            this.Log_DebugOnly("OnSave", ".");
            base.OnSave(gameNode);

            if (mainWindow != null) { mainWindow.Save(gameConfig); }
            if (logWindow != null) { logWindow.Save(gameConfig); }
            if (configWindow != null) { configWindow.Save(gameConfig); }
            if (rosterWindow != null) { rosterWindow.Save(gameConfig); }
            
            gameConfig.Save(gameNode);

            this.Log_DebugOnly("OnSave", "Saved keepfit persistence data");
//            this.Log_DebugOnly("OnSave", gameNode.ToString());
        }

        void OnDestroy()
        {
            this.Log_DebugOnly("OnDestroy", ".");
            if (appLauncherButton == null)
            {
                this.Log_DebugOnly("OnDestroy", "No appLauncher button to remove.");
            }
            else
            {
                this.Log_DebugOnly("OnDestroy", "Removing appLauncher button.");
                ApplicationLauncher.Instance.DisableMutuallyExclusive(appLauncherButton);
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                appLauncherButton = null;
            }
            
            if (toolmodLauncherButton != null)
            {
                toolmodLauncherButton.Destroy(); 
                toolmodLauncherButton = null;
            }
            

            if (this.crewFitnessController == null)
            {
                this.Log_DebugOnly("OnDestroy", "No crewFitness controller to destroy.");
            }
            else
            {
                this.Log_DebugOnly("OnDestroy", "Destroying crewFitness controller.");
                Destroy(crewFitnessController);
                crewFitnessController = null;
            }

            if (this.crewRosterController == null)
            {
                this.Log_DebugOnly("OnDestroy", "No crewRoster controller to destroy.");
            }
            else
            {
                this.Log_DebugOnly("OnDestroy", "Destroying crewRoster controller.");
                Destroy(crewRosterController);
                crewRosterController = null;
            }

            if (this.geeEffectsController == null)
            {
                this.Log_DebugOnly("OnDestroy", "No geeEffects controller to destroy.");
            }
            else
            {
                this.Log_DebugOnly("OnDestroy", "Destroying geeEffects controller.");
                Destroy(geeEffectsController);
                geeEffectsController = null;
            }

            if (mainWindow != null)
            {
                Destroy(mainWindow);
                mainWindow = null;
            }

            if (logWindow != null)
            {
                Destroy(logWindow);
                logWindow = null;
            }

            if (configWindow != null)
            {
                Destroy(configWindow);
                configWindow = null;
            }

            if (rosterWindow != null)
            {
                Destroy(rosterWindow);
                rosterWindow = null;
            }

            if (allVesselsWindow == null)
            {
                Destroy(allVesselsWindow);
                allVesselsWindow = null;
            }

        }
    }
}
