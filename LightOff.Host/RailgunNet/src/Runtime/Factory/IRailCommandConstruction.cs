using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;

namespace RailgunNet.Factory
{
    public interface IRailCommandConstruction
    {
        RailCommand CreateCommand();
        RailCommandUpdate CreateCommandUpdate();
    }
}
