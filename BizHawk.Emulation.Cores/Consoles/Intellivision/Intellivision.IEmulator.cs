﻿using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ControllerDefinition ControllerDefinition
		{
			get { return ControllerDeck.Definition; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (Tracer.Enabled)
				_cpu.TraceCallback = (s) => Tracer.Put(s);
			else
				_cpu.TraceCallback = null;

			//reset the count of audio samples
			_psg.sample_count = 0;

			_frame++;
			stic_row = -1;
			// read the controller state here for now
			get_controller_state();

			// this timer tracks cycles stolen by the STIC during the visible part of the frame, quite a large number of them actually
			int delay_cycles = 600; 
			int delay_timer = -1;

			_cpu.PendingCycles = (14934 - 3791 + _cpu.GetPendingCycles());
			_stic.Sr1 = true;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);

				if (delay_cycles>=0 && _stic.active_display)
					delay_cycles += cycles;

				if (delay_timer> 0 && _stic.active_display)
				{
					delay_timer -= cycles;
					if (delay_timer<=0)
					{
						_stic.ToggleSr2();
						delay_cycles = 0;
					}
				}

				if (delay_cycles>= 750 && _stic.active_display)
				{
					delay_cycles = -1;
					delay_timer = 110;
					_stic.ToggleSr2();
					if (stic_row >= 0)
					{
						_stic.in_vb_2 = true;
						_stic.Background(stic_row);
						_stic.in_vb_2 = false;
					}					
					stic_row++;
				}
				Connect();
			}

			// set up VBlank variables
			_stic.in_vb_1 = true;
			_stic.in_vb_2 = true;

			if (_stic.active_display)
			{
				_stic.Mobs();
			}

			_stic.active_display = false;
			_stic.Sr1 = false;

			_cpu.PendingCycles = (2900 + _cpu.GetPendingCycles());

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				//_psg.generate_sound(cycles);
				Connect();
			}

			// vblank phase 2
			_cpu.PendingCycles = (891 + _cpu.GetPendingCycles());
			_stic.in_vb_1 = false;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				//_psg.generate_sound(cycles);
				Connect();
			}
			_stic.in_vb_2 = false;

		}

		private int _frame;
		private int stic_row;
		public int Frame { get { return _frame; } }

		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get { return true; } }

		[FeatureNotImplemented]
		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			_frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{

		}
	}
}
