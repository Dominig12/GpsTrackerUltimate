using DMISharp;

namespace GPSTrackerUltimate.Types.Enum
{

    public enum DirectionBYond
    {

        /// <summary>
        /// Southern (typically downward) direction
        /// </summary>
        South = 1,

        /// <summary>
        /// Northern (typically upwards) direction
        /// </summary>
        North = 2,

        /// <summary>
        /// Eastern (typically right) direction
        /// </summary>
        East = 4,

        /// <summary>
        /// Western (typically left) direction
        /// </summary>
        West = 8,

        /// <summary>
        /// Southeastern (typically down-right) direction
        /// </summary>
        SouthEast = 5,

        /// <summary>
        /// Southwestern (typically down-left) direction
        /// </summary>
        SouthWest = 9,

        /// <summary>
        /// Northeastern (typically up-right) direction
        /// </summary>
        NorthEast = 6,

        /// <summary>
        /// Northwestern (typically up-left) direction
        /// </summary>
        NorthWest = 10

    }

    public static class ConverterDirection
    {

        public static StateDirection ConvertByondDirToDmi(
            DirectionBYond dir )
        {
            switch ( dir )
            {
                case DirectionBYond.South:
                    return StateDirection.South;
                case DirectionBYond.North:
                    return StateDirection.North;
                case DirectionBYond.East:
                    return StateDirection.East;
                case DirectionBYond.West:
                    return StateDirection.West;
                case DirectionBYond.SouthEast:
                    return StateDirection.SouthEast;
                case DirectionBYond.SouthWest:
                    return StateDirection.SouthWest;
                case DirectionBYond.NorthEast:
                    return StateDirection.NorthEast;
                case DirectionBYond.NorthWest:
                    return StateDirection.NorthWest;
                default:
                    return StateDirection.South;
            }
        }

    }

}
