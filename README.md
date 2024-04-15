# Experimental Gameboy and Gameboy Color emulator written in C#

Checklist:
-  ~~Memory mapping with switchable banks~~
-  ~~CPU Register implementation~~
-  ~~245 standard CPU opcodes implemented~~
-  ~~Passing CPU json unit tests~~ All CPU tests are passing now!
-  ~~Testing CPU instructions with blargg's test rom via serial port (+ custom debugger)~~
    - All tests are passing except for cpu_instr (MBC1 required) and 02-interrupts (timers and interrupts need some more work)
-  Implementing all timers and interrupt handling
-  Keymap
-  Implementing PPU and VBlank behavior
-  Running first MBC0 titles
-  Emulating sound
-  RTC
-  Extra Mappers (MBC1, MBC3 and MBC5 at the very least)
-  GB->GBC modes switching
-  Adding GBC features
-  Save/Load state
-  Bugfixing, peripherals, improving accuracy
