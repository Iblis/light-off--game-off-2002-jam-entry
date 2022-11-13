using RailgunNet.System.Encoding;
using RailgunNet.System.Encoding.Compressors;
using System.Text;

namespace LightOff.Messaging
{
    public class CoordinateCompressor : RailFloatCompressor
    {
        public const float COORDINATE_PRECISION = 0.001f;
        public CoordinateCompressor() : base(
                -512.0f,
                512.0f,
                COORDINATE_PRECISION / 10.0f)
        {
        }

        [Encoder]
        public void Write(RailBitBuffer buffer, float f)
        {
            buffer.WriteFloat(this, f);
        }

        [Decoder]
        public float Read(RailBitBuffer buffer)
        {
            return buffer.ReadFloat(this);
        }
    }
}
