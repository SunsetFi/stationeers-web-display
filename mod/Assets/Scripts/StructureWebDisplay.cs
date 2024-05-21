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
			// if (webDisplay == null)
			// {
			// 	Debug.Log("WebDisplayBehavior is missing!!!");
			// 	webDisplay = gameObject.AddComponent<WebDisplayBehavior>();
			// }

			webDisplay.Url = "http://localhost:8080/#/screens/demo"; //"https://www.youtube.com/embed/EAO7uZSew74?si=BhctKMWFLgVnRZNc";
		}
	}
}