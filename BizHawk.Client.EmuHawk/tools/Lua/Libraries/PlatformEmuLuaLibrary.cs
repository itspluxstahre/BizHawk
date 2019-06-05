using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

using JetBrains.Annotations;

namespace BizHawk.Client.EmuHawk
{
	public abstract class PlatformEmuLuaLibrary
	{
		public LuaDocumentation Docs { get; protected set; }

		public GuiLuaLibrary GuiLibrary => (GuiLuaLibrary) Libraries[typeof(GuiLuaLibrary)];

		public bool IsRebootingCore { get; set; } // pretty hacky.. we dont want a lua script to be able to restart itself by rebooting the core

		[NotNull] protected readonly Dictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();

		public EventWaitHandle LuaWait { get; protected set; }

		public IEnumerable<LuaFile> RunningScripts
		{
			get { return ScriptList.Where(lf => lf.Enabled); }
		}

		[NotNull] public readonly LuaFileList ScriptList = new LuaFileList();

		public abstract void CallExitEvent(LuaFile lf);
		public abstract void CallFrameAfterEvent();
		public abstract void CallFrameBeforeEvent();
		public abstract void CallLoadStateEvent(string name);
		public abstract void CallSaveStateEvent(string name);
		public abstract void Close();
		public abstract void EndLuaDrawing();
		public abstract void ExecuteString(string command);
		public abstract LuaFunctionList GetRegisteredFunctions();
		public abstract void Restart(IEmulatorServiceProvider newServiceProvider);
		public abstract EmuLuaLibrary.ResumeResult ResumeScriptFromThreadOf(LuaFile lf);
		public abstract void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
		public abstract void StartLuaDrawing();
		public abstract void WindowClosed(IntPtr handle);
	}
}