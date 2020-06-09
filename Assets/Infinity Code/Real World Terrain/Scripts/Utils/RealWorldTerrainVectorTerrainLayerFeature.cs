using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain
{
    [Serializable]
    public class RealWorldTerrainVectorTerrainLayerFeature
    {
#if UNITY_2018_3_OR_NEWER
        private static string[] _layerNames;
        private static List<string> _landuseNames;
        private static List<string> _landuseOverlayNames;
        private static List<string> _waterwayNames;

        public TerrainLayer terrainLayer;
        public List<Rule> rules;

        public static List<string> landuseNames
        {
            get
            {
                if (_landuseNames == null) _landuseNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLanduse)).ToList();
                return _landuseNames;
            }
        }

        public static List<string> landuseOverlayNames
        {
            get
            {
                if (_landuseOverlayNames == null) _landuseOverlayNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLanduseOverlay)).ToList();
                return _landuseOverlayNames;
            }
        }

        public static string[] layerNames
        {
            get
            {
                if (_layerNames == null) _layerNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLayer));
                return _layerNames;
            }
        }

        public static List<string> waterwayNames
        {
            get
            {
                if (_waterwayNames == null) _waterwayNames = Enum.GetNames(typeof(RealWorldTerrainMapboxWaterway)).ToList();
                return _waterwayNames;
            }
        }
#endif

        [Serializable]
        public class Rule
        {
            public RealWorldTerrainMapboxLayer layer = RealWorldTerrainMapboxLayer.building;
            public int extra = ~0;

            public bool hasExtra
            {
                get
                {
                    return layer == RealWorldTerrainMapboxLayer.landuse_overlay ||
                           layer == RealWorldTerrainMapboxLayer.landuse ||
                           layer == RealWorldTerrainMapboxLayer.waterway;
                }
            }
        }
    }
}