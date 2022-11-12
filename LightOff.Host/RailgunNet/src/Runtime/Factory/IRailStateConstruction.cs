using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Encoding.Compressors;

namespace RailgunNet.Factory
{
    public interface IRailStateConstruction
    {
        RailIntCompressor EntityTypeCompressor { get; }
        RailState CreateState(int factoryType);
        RailStateDelta CreateDelta();
        RailStateRecord CreateRecord();
    }
}
