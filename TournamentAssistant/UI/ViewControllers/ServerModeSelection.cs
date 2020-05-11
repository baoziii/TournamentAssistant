﻿#pragma warning disable 0649
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using TMPro;
using TournamentAssistantShared;
using UnityEngine.UI;

namespace TournamentAssistant.UI.ViewControllers
{
    class ServerModeSelection : BSMLResourceViewController
    {
        public override string ResourceName => $"TournamentAssistant.UI.Views.{GetType().Name}.bsml";

        public event Action TournamentButtonPressed;
        public event Action TournamentAssistantButtonPressed;

        [UIComponent("status-text")]
        private TextMeshProUGUI statusText;

        [UIComponent("tournament-button")]
        private Button _tournamentRoomButton;

        [UIComponent("TournamentAssistant-button")]
        private Button _TournamentAssistantButton;

        //We need to keep track of the text like this because it is very possible
        //that we'll want to update it before the list is actually displayed.
        //This way, we can handle that situation and avoid null exceptions / missing data
        private string _statusText;
        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                if (statusText != null) statusText.text = _statusText;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);
            if (type == ActivationType.AddedToHierarchy)
            {
                if (_statusText != null) statusText.text = _statusText;
                _tournamentRoomButton.interactable = false;
                _TournamentAssistantButton.interactable = false;
            }
        }

        [UIAction("tournament-button-pressed")]
        private void TournamentButtonPress()
        {
            TournamentButtonPressed?.Invoke();
        }

        [UIAction("TournamentAssistant-button-pressed")]
        private void TournamentAssistantButtonPress()
        {
            TournamentAssistantButtonPressed?.Invoke();
        }

        public void EnableButtons()
        {
            _tournamentRoomButton.interactable = true;
            _TournamentAssistantButton.interactable = true;
        }

        public void DisableButtons()
        {
            _tournamentRoomButton.interactable = false;
            _TournamentAssistantButton.interactable = false;
        }
    }
}