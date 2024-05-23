using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using StationeersMods.Interface;
using UnityEngine;

namespace StationeersWebDisplay
{
	public class StructureWebDisplay : SmallDevice
	{
		private static string RootUrl = "http://localhost:8080/#";
		public override void Awake()
		{
			base.Awake();

			var webDisplay = gameObject.GetComponent<WebDisplayBehavior>();

			this.SetUrl();
			webDisplay.Enabled = this.Powered;
		}

		public override void OnInteractableUpdated(Interactable interactable)
		{
			base.OnInteractableUpdated(interactable);

			if (interactable.Action == InteractableType.Powered)
			{
				this.PoweredChanged();
			}
		}

		private void PoweredChanged()
		{
			var webDisplay = gameObject.GetComponent<WebDisplayBehavior>();
			if (this.Powered)
			{
				// We use a shared power and data cable, and I can't find an event to indicate when the data connection has changed.
				// Update the url here.
				this.SetUrl();
				webDisplay.Enable();
			}
			else
			{
				webDisplay.Disable();
			}
		}

		private void SetUrl()
		{
			var webDisplay = gameObject.GetComponent<WebDisplayBehavior>();

			var url = RootUrl + $"/displays/{this.ReferenceId}";

			webDisplay.Url = url;
		}
	}
}