using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using OpenTK.Input;

namespace BizHawk.Client.EmuHawk
{
	public class OTK_GamePad
	{
		//Note: OpenTK has both Gamepad and Joystick classes. An OpenTK Gamepad is a simplified version of Joystick
		//with pre-defined features that match an XInput controller. They did this to mimic XNA's controller API.
		//We're going to use Joystick directly, because it gives us full access to all possible buttons.
		//And it looks like GamePad itself isn't supported on OpenTK OS X.

		public static List<OTK_GamePad> Devices;
		private const int MAX_JOYSTICKS = 4; //They don't have a way to query this for some reason. 4 is the minimum promised.

		public static void Initialize()
		{
			Devices = new List<OTK_GamePad>();

			for (int i = 0; i < MAX_JOYSTICKS; i++)
			{
				JoystickState jss = Joystick.GetState(i);
				if (jss.IsConnected)
				{
					Console.WriteLine($"joydevice index: {i}"); //OpenTK doesn't expose the GUID, even though it stores it internally...

					OTK_GamePad ogp = new OTK_GamePad(i);
					Devices.Add(ogp);
				}
			}

		}

		public static void UpdateAll()
		{
			foreach (var device in Devices)
				device.Update();
		}

		public static void CloseAll()
		{
			if (Devices != null)
			{
				Devices.Clear();
			}
		}

		// ********************************** Instance Members **********************************

		readonly Guid _guid;
		readonly int _stickIdx;
		JoystickState state = new JoystickState();

		OTK_GamePad(int index)
		{
			_guid = Guid.NewGuid();
			_stickIdx = index;
			Update();
			InitializeCallbacks();
		}

		public void Update()
		{
			state = Joystick.GetState(_stickIdx);
		}

		public IEnumerable<Tuple<string, float>> GetFloats()
		{
			for (int pi = 0; pi < 64; pi++)
				yield return new Tuple<string, float>(pi.ToString(), 10.0f * state.GetAxis(pi));
		}

		/// <summary>FOR DEBUGGING ONLY</summary>
		public JoystickState GetInternalState()
		{
			return state;
		}

		public string Name { get { return $"Joystick {_stickIdx}"; } }
		public Guid Guid { get { return _guid; } }


		public string ButtonName(int index)
		{
			return names[index];
		}
		public bool Pressed(int index)
		{
			return actions[index]();
		}
		public int NumButtons { get; private set; }

		[NotNull] private readonly List<string> names = new List<string>();
		[NotNull] private readonly List<Func<bool>> actions = new List<Func<bool>>();

		void AddItem(string _name, Func<bool> callback)
		{
			names.Add(_name);
			actions.Add(callback);
			NumButtons++;
		}

		void InitializeCallbacks()
		{
			const int dzp = 400;
			const int dzn = -400;

			names.Clear();
			actions.Clear();
			NumButtons = 0;

			AddItem("X+", () => state.GetAxis(0) >= dzp);
			AddItem("X-", () => state.GetAxis(0) <= dzn);
			AddItem("Y+", () => state.GetAxis(1) >= dzp);
			AddItem("Y-", () => state.GetAxis(1) <= dzn);
			AddItem("Z+", () => state.GetAxis(2) >= dzp);
			AddItem("Z-", () => state.GetAxis(2) <= dzn);

			// Enjoy our delicious sliders. They're smaller than regular burgers but cost more.

			int jb = 1;
			for (int i = 0; i < 64; i++)
			{
				AddItem($"B{jb}", () => state.GetButton(i)==ButtonState.Pressed);
				jb++;
			}

			jb = 1;
			foreach (JoystickHat enval in Enum.GetValues(typeof(JoystickHat)))
			{
				AddItem($"POV{jb}U", () => state.GetHat(enval).IsUp);
				AddItem($"POV{jb}D", () => state.GetHat(enval).IsDown);
				AddItem($"POV{jb}L", () => state.GetHat(enval).IsLeft);
				AddItem($"POV{jb}R", () => state.GetHat(enval).IsRight);
				jb++;
			}
		}

		// Note that this does not appear to work at this time. I probably need to have more infos.
		public void SetVibration(int left, int right)
		{
			//Not supported in OTK Joystick. It is supported for OTK Gamepad, but I have to use Joystick for reasons mentioned above.
		}

	}
}

