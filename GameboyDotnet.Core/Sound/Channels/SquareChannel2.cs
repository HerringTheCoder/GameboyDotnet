using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;
using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound.Channels;

public class SquareChannel2(AudioBuffer audioBuffer) : BaseSquareChannel(audioBuffer);