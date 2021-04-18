﻿using BeatSaberMarkupLanguage;
using HMUI;
using System;
using System.Linq;
using TournamentAssistant.Misc;
using TournamentAssistant.UI.ViewControllers;
using TournamentAssistantShared.Models;
using TournamentAssistantShared.Models.Packets;

namespace TournamentAssistant.UI.FlowCoordinators
{
    class RoomSelectionCoordinator : FlowCoordinatorWithClient
    {
        private SplashScreen _splashScreen;
        private RoomSelection _roomSelection;
        private RoomCoordinator _roomCoordinator;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (addedToHierarchy)
            {
                //Set up UI
                SetTitle("大厅", ViewController.AnimationType.None);
                showBackButton = true;

                _roomSelection = BeatSaberUI.CreateViewController<RoomSelection>();
                _roomSelection.MatchSelected += RoomSelection_MatchSelected;
                _roomSelection.CreateMatchPressed += RoomSelection_MatchCreated;

                _splashScreen = BeatSaberUI.CreateViewController<SplashScreen>();
                _splashScreen.StatusText = $"正在连接服务器 \"{Host.Name}\"...";

                ProvideInitialViewControllers(_splashScreen);
            }

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        public override void Dismiss()
        {
            if (_roomCoordinator != null && IsFlowCoordinatorInHierarchy(_roomCoordinator)) _roomCoordinator.Dismiss();
            if (topViewController is RoomSelection) DismissViewController(topViewController, immediately: true);

            base.Dismiss();
        }

        protected override void Client_ConnectedToServer(ConnectResponse response)
        {
            base.Client_ConnectedToServer(response);
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _roomSelection.SetMatches(Plugin.client.State.Matches.ToList());
                PresentViewController(_roomSelection, immediately: true);
            });
        }

        protected override void Client_FailedToConnectToServer(ConnectResponse response)
        {
            base.Client_FailedToConnectToServer(response);
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _splashScreen.StatusText = !string.IsNullOrEmpty(response?.Message) ? response.Message : "初始化服务器连接失败，请重试...";
            });
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            Dismiss();
        }

        protected override void Client_MatchCreated(Match match)
        {
            base.Client_MatchCreated(match);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _roomSelection.SetMatches(Plugin.client.State.Matches.ToList());
            });
        }

        //The match list item is used in click events, so it needs to be up to date as possible
        //(ie: have the most recent player lists) This shouln't get score updates as they're only directed to match players
        //We could potentially refresh the view here too if we ever include updatable data in the texts
        protected override void Client_MatchInfoUpdated(Match natch)
        {
            base.Client_MatchInfoUpdated(natch);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _roomSelection.SetMatches(Plugin.client.State.Matches.ToList());
            });
        }

        protected override void Client_MatchDeleted(Match match)
        {
            base.Client_MatchDeleted(match);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                _roomSelection.SetMatches(Plugin.client.State.Matches.ToList());
            });
        }

        private void RoomSelection_MatchCreated()
        {
            var match = new Match()
            {
                Guid = Guid.NewGuid().ToString(),
                Leader = Plugin.client.Self,
                Players = new Player[] { Plugin.client.Self as Player }
            };

            Plugin.client.CreateMatch(match);

            _roomCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomCoordinator>();
            _roomCoordinator.DidFinishEvent += RoomCoordinator_DidFinishEvent;
            _roomCoordinator.Match = match;
            PresentFlowCoordinator(_roomCoordinator);
        }

        private void RoomCoordinator_DidFinishEvent()
        {
            _roomCoordinator.DidFinishEvent -= RoomCoordinator_DidFinishEvent;

            //If we're marked to dismiss ourself, we should do so as soon as our child coordinator returns to us
            void onComplete()
            {
                if (ShouldDismissOnReturnToMenu) Dismiss();
            }

            DismissFlowCoordinator(_roomCoordinator, finishedCallback: onComplete);
        }

        private void RoomSelection_MatchSelected(Match match)
        {
            _roomCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomCoordinator>();
            _roomCoordinator.DidFinishEvent += RoomCoordinator_DidFinishEvent;
            _roomCoordinator.Match = match;
            PresentFlowCoordinator(_roomCoordinator);

            //Add ourself to the match and send the update
            match.Players = match.Players.ToList().Union(new Player[] { Plugin.client.Self as Player }).ToArray();
            Plugin.client.UpdateMatch(match);

        }
    }
}
