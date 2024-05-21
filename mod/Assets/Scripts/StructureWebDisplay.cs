using System;
using System.Collections.Generic;
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
		public override void Awake()
		{
			base.Awake();

			var webDisplay = gameObject.GetComponent<WebDisplayBehavior>();

			webDisplay.Enabled = this.Powered;
			webDisplay.Url = "http://localhost:8080/#/screens/demo"; //"https://www.youtube.com/embed/EAO7uZSew74?si=BhctKMWFLgVnRZNc";
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
				webDisplay.Enable();
			}
			else
			{
				webDisplay.Disable();
			}
		}
	}
}