namespace EUFramework.Extension.EUCollision2D.Script.Core
{
    public struct EUConllision2DTool
    {
        /// <summary>
        /// 通过位掩码信息转换对应的Layer下标
        /// </summary>
        /// <param name="layer">传递位掩码int参数</param>
        /// <returns>返回该图层的下标</returns>
        public static int GetLayer(int layerMask) 
        {
            switch (layerMask)
            {
                case 1:
                    return 0;
                case 2:
                    return 1;
                case 4:
                    return 2;
                case 8:
                    return 3;
                case 16:
                    return 4;
                case 32:
                    return 5;
                case 64:
                    return 6;
                case 128:
                    return 7;
                case 256:
                    return 8;
                case 512:
                    return 9;
                case 1024:
                    return 10;
                case 2048:
                    return 11;
                case 4096:
                    return 12;
                case 8192:
                    return 13;
                case 16384:
                    return 14;
                case 32384:
                    return 15;
                case 64384:
                    return 16;
                case 128128:
                    return 17;
                case 256256:
                    return 18;
                case 512512:
                    return 19;
                case 1024128:
                    return 20;
                case 2048128:
                    return 21;
                case 4096128:
                    return 22;
                case 8192128:
                    return 23;
                case 16384128:
                    return 24;
                case 32384128:
                    return 25;
                case 64384128:
                    return 26;
                case 128128128:
                    return 27;
                case 256256128:
                    return 28;
                case 512512128:
                    return 29;
                case 1024128128:
                    return 30;
                case -2147483648:
                    return 31;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// 通过对应的下标信息转换为对应的位掩码信息
        /// </summary>
        /// <param name="layerIndex">传递对应的下标信息</param>
        /// <returns>返回该下标图层的位掩码</returns>
        public static int GetLayerMask(int layer)
        {
            switch (layer)
            {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 4;
                case 3:
                    return 8;
                case 4:
                    return 16;
                case 5:
                    return 32;
                case 6:
                    return 64;
                case 7:
                    return 128;
                case 8:
                    return 256;
                case 9:
                    return 512;
                case 10:
                    return 1024;
                case 11:
                    return 2048;
                case 12:
                    return 4096;
                case 13:
                    return 8192;
                case 14:
                    return 16384;
                case 15:
                    return 32384;
                case 16:
                    return 64384;
                case 17:
                    return 128128;
                case 18:
                    return 256256;
                case 19:
                    return 512512;
                case 20:
                    return 1024128;
                case 21:
                    return 2048128;
                case 22:
                    return 4096128;
                case 24:
                    return 8192128;
                case 25:
                    return 16384128;
                case 26:
                    return 32384128;
                case 27:
                    return 64384128;
                case 28:
                    return 128128128;
                case 29:
                    return 256256128;
                case 30:
                    return 512512128;
                case 31:
                    return 1024128128;
                case 32:
                    return -2147483648;
                default:
                    return -1;
            }
        }
    }
}