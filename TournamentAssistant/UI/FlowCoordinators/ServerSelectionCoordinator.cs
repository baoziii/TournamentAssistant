﻿using BeatSaberMarkupLanguage;
using HMUI;
using System.Linq;
using TournamentAssistant.UI.ViewControllers;
using TournamentAssistantShared.Models;

namespace TournamentAssistant.UI.FlowCoordinators
{
    class ServerSelectionCoordinator : FlowCoordinatorWithScrapedInfo, IFinishableFlowCoordinator
    {
        public FlowCoordinatorWithClient DestinationCoordinator { get; set; }

        private ServerSelection _serverSelectionViewController;
        private SplashScreen _splashScreen;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (addedToHierarchy)
            {
                //Set up UI
                SetTitle("服务器列表", ViewController.AnimationType.None);
                showBackButton = false;

                _splashScreen = BeatSaberUI.CreateViewController<SplashScreen>();
                _splashScreen.StatusText = "正在获取服务器列表...";

                ProvideInitialViewControllers(_splashScreen);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            if (removedFromHierarchy)
            {
                _serverSelectionViewController.ServerSelected -= serverSelectionViewController_selectedServer;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (topViewController is ServerSelection) DismissViewController(topViewController, immediately: true);

            base.Dismiss();
        }

        private void serverSelectionViewController_selectedServer(CoreServer host)
        {
            DestinationCoordinator.DidFinishEvent += destinationCoordinator_DidFinishEvent;
            DestinationCoordinator.Host = host;
            PresentFlowCoordinator(DestinationCoordinator);
        }

        private void destinationCoordinator_DidFinishEvent()
        {
            DestinationCoordinator.DidFinishEvent -= destinationCoordinator_DidFinishEvent;
            DismissFlowCoordinator(DestinationCoordinator);
        }

        protected override void OnIndividualInfoScraped(CoreServer host, State state, int count, int total) => UpdateScrapeCount(count, total);

        protected override void OnInfoScraped()
        {
            showBackButton = true;
            _serverSelectionViewController = BeatSaberUI.CreateViewController<ServerSelection>();
            _serverSelectionViewController.ServerSelected += serverSelectionViewController_selectedServer;
            _serverSelectionViewController.SetServers(ScrapedInfo.Keys.Union(ScrapedInfo.Values.Where(x => x.KnownHosts != null).SelectMany(x => x.KnownHosts)).ToList());
            PresentViewController(_serverSelectionViewController);
        }

        private void UpdateScrapeCount(int count, int total)
        {
            _splashScreen.StatusText = $"正在获取数据 ({count} / {total})...";
        }
    }
}
