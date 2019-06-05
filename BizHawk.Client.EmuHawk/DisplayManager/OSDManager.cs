using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.Common.InputAdapterExtensions;
using BizHawk.Bizware.BizwareGL;

using JetBrains.Annotations;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This is an old abstracted rendering class that the OSD system is using to get its work done.
	/// We should probably just use a GuiRenderer (it was designed to do that) although wrapping it with
	/// more information for OSDRendering could be helpful I suppose
	/// </summary>
	public interface IBlitter
	{
		IBlitterFont GetFontType(string fontType);
		void DrawString(string s, IBlitterFont font, Color color, float x, float y);
		SizeF MeasureString(string s, IBlitterFont font);
		Rectangle ClipBounds { get; set; }
	}

	class UIMessage
	{
		public string Message;
		public DateTime ExpireAt;
	}

	class UIDisplay
	{
		public string Message;
		public int X;
		public int Y;
		public int Anchor;
		public Color ForeColor;
		public Color BackGround;
	}

	public class OSDManager
	{
		public string FPS { get; set; }
		public IBlitterFont MessageFont;

		public void Dispose()
		{

		}

		public void Begin(IBlitter blitter)
		{
			MessageFont = blitter.GetFontType(nameof(MessageFont));
		}

		public Color FixedMessagesColor { get { return Color.FromArgb(Global.Config.MessagesColor); } }
		public Color FixedAlertMessageColor { get { return Color.FromArgb(Global.Config.AlertMessageColor); } }

		public OSDManager()
		{
		}

		private float GetX(IBlitter g, int x, int anchor, string message)
		{
			var size = g.MeasureString(message, MessageFont);

			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 2: //Bottom Left
					return x;
				case 1: //Top Right
				case 3: //Bottom Right
					return g.ClipBounds.Width - x - size.Width;
			}
		}

		private float GetY(IBlitter g, int y, int anchor, string message)
		{
			var size = g.MeasureString(message, MessageFont);

			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 1: //Top Right
					return y;
				case 2: //Bottom Left
				case 3: //Bottom Right
					return g.ClipBounds.Height - y - size.Height;
			}
		}

		private string MakeFrameCounter()
		{
			if (Global.MovieSession.Movie.IsFinished)
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount)
					.Append(" (Finished)");
				return sb.ToString();
			}

			if (Global.MovieSession.Movie.IsPlaying)
			{
				var sb = new StringBuilder();
				sb
					.Append(Global.Emulator.Frame)
					.Append('/')
					.Append(Global.MovieSession.Movie.FrameCount);

				return sb.ToString();
			}
			
			if (Global.MovieSession.Movie.IsRecording)
			{
				return Global.Emulator.Frame.ToString();
			}
			
			return Global.Emulator.Frame.ToString();
		}

		[NotNull] private List<UIMessage> messages = new List<UIMessage>(5);
		[NotNull] private List<UIDisplay> GUITextList = new List<UIDisplay>();

		public void AddMessage(string message)
		{
			messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

		public void AddGUIText(string message, int x, int y, Color backGround, Color foreColor, int anchor)
		{
			GUITextList.Add(new UIDisplay
			{
				Message = message,
				X = x,
				Y = y,
				BackGround = backGround,
				ForeColor = foreColor,
				Anchor = anchor
			});
		}

		public void ClearGUIText()
		{
			GUITextList.Clear();
		}

		public void DrawMessages(IBlitter g)
		{
			if (!Global.Config.DisplayMessages)
			{
				return;
			}

			messages.RemoveAll(m => DateTime.Now > m.ExpireAt);
			int line = 1;
			if (Global.Config.StackOSDMessages)
			{
				for (int i = messages.Count - 1; i >= 0; i--, line++)
				{
					float x = GetX(g, Global.Config.DispMessagex, Global.Config.DispMessageanchor, messages[i].Message);
					float y = GetY(g, Global.Config.DispMessagey, Global.Config.DispMessageanchor, messages[i].Message);
					if (Global.Config.DispMessageanchor < 2)
					{
						y += ((line - 1) * 18);
					}
					else
					{
						y -= ((line - 1) * 18);
					}

					//g.DrawString(messages[i].Message, MessageFont, Color.Black, x + 2, y + 2);
					g.DrawString(messages[i].Message, MessageFont, FixedMessagesColor, x, y);
				}
			}
			else
			{
				if (messages.Any())
				{
					int i = messages.Count - 1;

					float x = GetX(g, Global.Config.DispMessagex, Global.Config.DispMessageanchor, messages[i].Message);
					float y = GetY(g, Global.Config.DispMessagey, Global.Config.DispMessageanchor, messages[i].Message);
					if (Global.Config.DispMessageanchor < 2)
					{
						y += ((line - 1) * 18);
					}
					else
					{
						y -= ((line - 1) * 18);
					}

					//g.DrawString(messages[i].Message, MessageFont, Color.Black, x + 2, y + 2);
					g.DrawString(messages[i].Message, MessageFont, FixedMessagesColor, x, y);
				}
			}

			foreach (var text in GUITextList)
			{
				try
				{
					float posx = GetX(g, text.X, text.Anchor, text.Message);
					float posy = GetY(g, text.Y, text.Anchor, text.Message);

					//g.DrawString(text.Message, MessageFont, text.BackGround, posx + 2, posy + 2);
					g.DrawString(text.Message, MessageFont, text.ForeColor, posx, posy);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public string InputStrMovie()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);

			return lg.GenerateInputDisplay();
		}

		public string InputStrImmediate()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.AutofireStickyXORAdapter);

			return lg.GenerateInputDisplay();
		}

		public string InputPrevious()
		{
			if (Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished)
			{
				var lg = Global.MovieSession.LogGeneratorInstance();
				var state = Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1);
				if (state != null)
				{
					lg.SetSource(state);
					return lg.GenerateInputDisplay();
				}
			}

			return "";
		}

		public string InputStrOrAll()
		{
			var m = (Global.MovieSession.Movie.IsActive && 
				!Global.MovieSession.Movie.IsFinished &&
				Global.Emulator.Frame > 0) ?
				Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1) :
				Global.MovieSession.MovieControllerInstance();

			var lg = Global.MovieSession.LogGeneratorInstance();

			lg.SetSource(Global.AutofireStickyXORAdapter.Or(m));
			return lg.GenerateInputDisplay();
		}

		public string InputStrSticky()
		{
			var stickyOr = new StickyOrAdapter
			{
				Source = Global.StickyXORAdapter,
				SourceStickyOr = Global.AutofireStickyXORAdapter
			};

			return MakeStringFor(stickyOr);
		}

		private string MakeStringFor(IController controller)
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(controller);
			return lg.GenerateInputDisplay();
		}

		public string MakeIntersectImmediatePrevious()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				var m = Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished ?
					Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1) :
					Global.MovieSession.MovieControllerInstance();

				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(Global.AutofireStickyXORAdapter.And(m));
				return lg.GenerateInputDisplay();
			}

			return "";
		}

		public string MakeRerecordCount()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				return Global.MovieSession.Movie.Rerecords.ToString();
			}
			
			return "";
		}

		private void DrawOsdMessage(IBlitter g, string message, Color color, float x, float y)
		{
			//g.DrawString(message, MessageFont, Color.Black, x + 1, y + 1);
			g.DrawString(message, MessageFont, color, x, y);
		}

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo(IBlitter g)
		{
			if (Global.Config.DisplayFrameCounter && !Global.Game.IsNullInstance)
			{
				string message = MakeFrameCounter();
				float x = GetX(g, Global.Config.DispFrameCx, Global.Config.DispFrameanchor, message);
				float y = GetY(g, Global.Config.DispFrameCy, Global.Config.DispFrameanchor, message);

				DrawOsdMessage(g, message, Color.FromArgb(Global.Config.MessagesColor), x, y);

				if (GlobalWin.MainForm.IsLagFrame)
				{
					DrawOsdMessage(g, Global.Emulator.Frame.ToString(), FixedAlertMessageColor, x, y);
				}
			}

			if (Global.Config.DisplayInput && !Global.Game.IsNullInstance)
			{
				if ((Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
					|| (Global.MovieSession.Movie.IsFinished && Global.Emulator.Frame == Global.MovieSession.Movie.InputLogLength)) // Account for the last frame of the movie, the movie state is immediately "Finished" here but we still want to show the input
				{
					var input = InputStrMovie();
					var x = GetX(g, Global.Config.DispInpx, Global.Config.DispInpanchor, input);
					var y = GetY(g, Global.Config.DispInpy, Global.Config.DispInpanchor, input);
					Color c = Color.FromArgb(Global.Config.MovieInput);
					//g.DrawString(input, MessageFont, Color.Black, x + 1, y + 1);
					g.DrawString(input, MessageFont, c, x, y);
				}

				else // TODO: message config -- allow setting of "previous", "mixed", and "auto"
				{
					var previousColor = Color.Orange;
					Color immediateColor = Color.FromArgb(Global.Config.MessagesColor);
					var autoColor = Color.Pink;
					var changedColor = Color.PeachPuff;

					//we need some kind of string for calculating position when right-anchoring, of something like that
					var bgStr = InputStrOrAll();
					var x = GetX(g, Global.Config.DispInpx, Global.Config.DispInpanchor, bgStr);
					var y = GetY(g, Global.Config.DispInpy, Global.Config.DispInpanchor, bgStr);

					//now, we're going to render these repeatedly, with higher-priority things overriding

					//first display previous frame's input.
					//note: that's only available in case we're working on a movie
					var previousStr = InputPrevious();
					g.DrawString(previousStr, MessageFont, previousColor, x, y);

					//next, draw the immediate input.
					//that is, whatever's being held down interactively right this moment even if the game is paused
					//this includes things held down due to autohold or autofire
					//I know, this is all really confusing
					var immediate = InputStrImmediate();
					g.DrawString(immediate, MessageFont, immediateColor, x, y);

					//next draw anything that's pressed because it's sticky.
					//this applies to autofire and autohold both. somehow. I dont understand it.
					//basically we're tinting whatever's pressed because it's sticky specially
					//in order to achieve this we want to avoid drawing anything pink that isnt actually held down right now
					//so we make an AND adapter and combine it using immediate & sticky
					var autoString = MakeStringFor(Global.StickyXORAdapter.Source.Xor(Global.AutofireStickyXORAdapter).And(Global.AutofireStickyXORAdapter));
					g.DrawString(autoString, MessageFont, autoColor, x, y);

					//recolor everything that's changed from the previous input
					var immediateOverlay = MakeIntersectImmediatePrevious();
					g.DrawString(immediateOverlay, MessageFont, changedColor, x, y);
				}
			}

			if (Global.MovieSession.MultiTrack.IsActive)
			{
				float x = GetX(g, Global.Config.DispMultix, Global.Config.DispMultianchor, Global.MovieSession.MultiTrack.Status);
				float y = GetY(g, Global.Config.DispMultiy, Global.Config.DispMultianchor, Global.MovieSession.MultiTrack.Status);

				DrawOsdMessage(g, Global.MovieSession.MultiTrack.Status, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayFPS && FPS != null)
			{
				float x = GetX(g, Global.Config.DispFPSx, Global.Config.DispFPSanchor, FPS);
				float y = GetY(g, Global.Config.DispFPSy, Global.Config.DispFPSanchor, FPS);

				DrawOsdMessage(g, FPS, FixedMessagesColor, x, y);
			}

			if (Global.Config.DisplayLagCounter && Global.Emulator.CanPollInput())
			{
				var counter = Global.Emulator.AsInputPollable().LagCount.ToString();
				var x = GetX(g, Global.Config.DispLagx, Global.Config.DispLaganchor, counter);
				var y = GetY(g, Global.Config.DispLagy, Global.Config.DispLaganchor, counter);

				DrawOsdMessage(g, counter, FixedAlertMessageColor, x, y);
			}

			if (Global.Config.DisplayRerecordCount)
			{
				string rerec = MakeRerecordCount();
				float x = GetX(g, Global.Config.DispRecx, Global.Config.DispRecanchor, rerec);
				float y = GetY(g, Global.Config.DispRecy, Global.Config.DispRecanchor, rerec);

				DrawOsdMessage(g, rerec, FixedMessagesColor, x, y);
			}

			if (Global.ClientControls["Autohold"] || Global.ClientControls["Autofire"])
			{
				var disp = new StringBuilder("Held: ");

				foreach (string sticky in Global.StickyXORAdapter.CurrentStickies)
				{
					disp.Append(sticky).Append(' ');
				}

				foreach (string autoSticky in Global.AutofireStickyXORAdapter.CurrentStickies)
				{
					disp
						.Append("Auto-")
						.Append(autoSticky)
						.Append(' ');
				}

				var message = disp.ToString();

				g.DrawString(
					message,
					MessageFont,
					Color.White,
					GetX(g, Global.Config.DispAutoholdx, Global.Config.DispAutoholdanchor, message),
					GetY(g, Global.Config.DispAutoholdy, Global.Config.DispAutoholdanchor, message));
			}

			if (Global.MovieSession.Movie.IsActive && Global.Config.DisplaySubtitles)
			{
				var subList = Global.MovieSession.Movie.Subtitles.GetSubtitles(Global.Emulator.Frame);

				foreach (var sub in subList)
				{
					DrawOsdMessage(g, sub.Message, Color.FromArgb((int)sub.Color), sub.X, sub.Y);
				}
			}
		}
	}

}