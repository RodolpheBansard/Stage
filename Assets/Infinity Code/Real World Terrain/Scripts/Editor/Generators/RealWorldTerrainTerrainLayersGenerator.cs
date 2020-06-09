/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using InfinityCode.RealWorldTerrain.Vector;
using InfinityCode.RealWorldTerrain.Phases;
using InfinityCode.RealWorldTerrain.Windows;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Generators
{
    using LPoints = List<RealWorldTerrainVectorTile.LPoint>;

    public static class RealWorldTerrainTerrainLayersGenerator
    {
#if UNITY_2018_3_OR_NEWER
        private static Dictionary<ulong, RealWorldTerrainVectorTile> tiles;
        private static int maxTextureLevel;

        private static RealWorldTerrainPrefs prefs
        {
            get { return RealWorldTerrainWindow.prefs; }
        }

        public static void Dispose()
        {
            if (tiles != null)
            {
                foreach (var pair in tiles) pair.Value.Dispose();
                tiles = null;
            }
        }

        public static bool IsClockwise(Vector3[] points, int count)
        {
            double sum = 0d;
            for (int i = 0; i < count; i++)
            {
                Vector3 v1 = points[i];
                Vector3 v2 = points[(i + 1) % count];
                sum += (v2.x - v1.x) * (v2.z + v1.z);
            }

            return sum > 0d;
        }

        public static void Generate(RealWorldTerrainItem item)
        {
            int size = 1 << maxTextureLevel;

            int res = item.prefs.controlTextureResolution;
            int s = res / 256;

            TerrainLayer[] terrainLayers = new TerrainLayer[item.prefs.vectorTerrainLayers.Count + 1];
            terrainLayers[0] = item.prefs.vectorTerrainBaseLayer;
            for (int i = 0; i < item.prefs.vectorTerrainLayers.Count; i++) terrainLayers[i + 1] = item.prefs.vectorTerrainLayers[i].terrainLayer;

            item.terrainData.terrainLayers = terrainLayers;
            float[,,] map = new float[item.prefs.controlTextureResolution, item.prefs.controlTextureResolution, terrainLayers.Length - 1];

            double rangeX = item.prefs.controlTextureResolution / (item.rightMercator - item.leftMercator) / size;
            double rangeY = item.prefs.controlTextureResolution / (item.bottomMercator - item.topMercator) / size;

            Vector3[] ps = new Vector3[64];

            foreach (var pair in tiles)
            {
                var tile = pair.Value;

                if (item.rightMercator * size < tile.x) continue;
                if (item.bottomMercator * size < tile.y) continue;
                if (item.leftMercator * size > tile.x + 1) continue;
                if (item.topMercator * size > tile.y + 1) continue;

                double sx = (tile.x - item.leftMercator * size) * rangeX;
                double ex = sx + rangeX;
                double sy = (tile.y - item.topMercator * size) * rangeY;
                double ey = sy + rangeY;

                int isx = (int) Math.Round(sx);
                int iex = (int) Math.Round(ex);
                int isy = (int) Math.Round(sy);
                int iey = (int) Math.Round(ey);

                if (isx < 0) isx = 0;
                if (isy < 0) isy = 0;
                if (iex >= res) iex = res;
                if (iey >= res) iey = res;

                tile.Load();

                List<string> layers = tile.GetLayerNames();
                for (int l = layers.Count - 1; l >= 0; l--)
                {
                    string layerName = layers[l];
                    if (layerName.Contains("label")) continue;

                    RealWorldTerrainVectorTile.Layer layer = tile.GetLayer(layerName);

                    for (int f = 0; f < layer.featureCount; f++)
                    {
                        RealWorldTerrainVectorTile.Feature feature = layer.GetFeature(f);
                        if (feature.geometryType != RealWorldTerrainVectorTile.GeomType.POLYGON) continue;

                        int mi = GetVectorTerrainLayerIndex(item, layer, feature);
                        if (mi == -1) continue;

                        List<LPoints> geometry = feature.Geometry(0);
                        if (geometry.Count == 0) continue;

                        foreach (LPoints points in geometry)
                        {
                            if (points.Count < 3) continue;

                            int count = points.Count;
                            if (count > ps.Length) Array.Resize(ref ps, Mathf.NextPowerOfTwo(count));

                            float minX = float.MaxValue;
                            float maxX = float.MinValue;
                            float minZ = float.MaxValue;
                            float maxZ = float.MinValue;

                            for (int i = 0; i < count; i++)
                            {
                                RealWorldTerrainVectorTile.LPoint p = points[i];
                                Vector3 wp = ps[i] = new Vector3((float) (ex - sx) * p.x / layer.extent + (float) sx, 0, (float) (ey - sy) * p.y / layer.extent + (float) sy);
                                if (wp.x < minX) minX = wp.x;
                                if (wp.z < minZ) minZ = wp.z;
                                if (wp.x > maxX) maxX = wp.x;
                                if (wp.z > maxZ) maxZ = wp.z;
                            }

                            if (maxX < 0 || maxZ < 0) continue;
                            if (minX >= res || minZ >= res) continue;

                            int csx = isx;
                            int csy = isy;
                            int cex = iex;
                            int cey = iey;

                            if (csx < minX) csx = (int) minX;
                            if (csy < minZ) csy = (int) minZ;
                            if (cex > maxX) cex = Mathf.CeilToInt(maxX);
                            if (cey > maxZ) cey = Mathf.CeilToInt(maxZ);

                            bool isClockWise = IsClockwise(ps, count);
                            float v = !isClockWise ? 1 : 0;

                            for (int x = csx; x < cex; x++)
                            {
                                for (int y = csy; y < cey; y++)
                                {
                                    if (IsPointInPolygon(ps, count, x, y)) map[res - y - 1, x, mi] = v;
                                }
                            }
                        }
                    }
                }

                tile.Dispose();
            }

            float[,,] finalMap = new float[item.prefs.controlTextureResolution, item.prefs.controlTextureResolution, terrainLayers.Length];

            float m1 = 0.125f;
            float m2 = 0.0625f;
            bool isLastLayer = true;

            for (int i = terrainLayers.Length - 1; i >= 0; i--)
            {
                int mi = i - 1;
                for (int x = 0; x < res; x++)
                {
                    for (int y = 0; y < res; y++)
                    {
                        float v = 1;
                        float w = 1;

                        if (i > 0)
                        {
                            v = map[y, x, mi];

                            if (x > 0)
                            {
                                v += map[y, x - 1, mi];
                                w += m1;

                                if (y > 0)
                                {
                                    v += map[y - 1, x - 1, mi];
                                    w += m2;
                                }

                                if (y < res - 1)
                                {
                                    v += map[y + 1, x - 1, mi];
                                    w += m2;
                                }
                            }

                            if (x < res - 1)
                            {
                                v += map[y, x + 1, mi];
                                w += m1;

                                if (y > 0)
                                {
                                    v += map[y - 1, x + 1, mi];
                                    w += m2;
                                }

                                if (y < res - 1)
                                {
                                    v += map[y + 1, x + 1, mi];
                                    w += m2;
                                }
                            }

                            if (y > 0)
                            {
                                v += map[y - 1, x, mi];
                                w += m1;
                            }

                            if (y < res - 1)
                            {
                                v += map[y + 1, x, mi];
                                w += m1;
                            }
                        }

                        if (isLastLayer) finalMap[y, x, i] = v / w;
                        else
                        {
                            float remain = 1;
                            for (int j = terrainLayers.Length - 1; j > i; j--) remain -= finalMap[y, x, j];

                            if (remain > 0) v = remain * v / w;
                            else v = 0;

                            finalMap[y, x, i] = v;
                        }
                    }
                }

                isLastLayer = false;
            }

            item.terrainData.SetAlphamaps(0, 0, finalMap);
            RealWorldTerrainPhase.phaseComplete = true;
        }

        private static int GetVectorTerrainLayerIndex(RealWorldTerrainItem item, RealWorldTerrainVectorTile.Layer layer, RealWorldTerrainVectorTile.Feature feature)
        {
            string layerName = layer.name;

            var layers = item.prefs.vectorTerrainLayers;
            for (int i = 0; i < layers.Count; i++)
            {
                var l = layers[i];

                for (int j = 0; j < l.rules.Count; j++)
                {
                    var rule = l.rules[j];
                    if (rule.layer.ToString() == layerName)
                    {
                        if (!rule.hasExtra || rule.extra == ~0) return i;

                        Dictionary<string, object> properties = feature.GetProperties();
                        object fClass;
                        if (properties.TryGetValue("class", out fClass))
                        {
                            if (rule.layer == RealWorldTerrainMapboxLayer.landuse)
                            {
                                int index = RealWorldTerrainVectorTerrainLayerFeature.landuseNames.IndexOf((string) fClass);
                                BitArray ba = new BitArray(BitConverter.GetBytes(rule.extra));
                                if (ba.Get(index)) return i;
                            }
                            else if (rule.layer == RealWorldTerrainMapboxLayer.landuse_overlay)
                            {
                                int index = RealWorldTerrainVectorTerrainLayerFeature.landuseOverlayNames.IndexOf((string)fClass);
                                BitArray ba = new BitArray(BitConverter.GetBytes(rule.extra));
                                if (ba.Get(index)) return i;
                            }
                            else if (rule.layer == RealWorldTerrainMapboxLayer.waterway)
                            {
                                int index = RealWorldTerrainVectorTerrainLayerFeature.waterwayNames.IndexOf((string)fClass);
                                BitArray ba = new BitArray(BitConverter.GetBytes(rule.extra));
                                if (ba.Get(index)) return i;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        public static void Init()
        {
            int textureLevel;
            if (prefs.maxTextureLevel == 0)
            {
                textureLevel = 0;

                int tx = prefs.controlTextureResolution * prefs.terrainCount.x / 256;
                int ty = prefs.controlTextureResolution * prefs.terrainCount.y / 256;

                for (int z = 5; z < 24; z++)
                {
                    double stx, sty, etx, ety;
                    RealWorldTerrainUtils.LatLongToTile(prefs.leftLongitude, prefs.topLatitude, z, out stx, out sty);
                    RealWorldTerrainUtils.LatLongToTile(prefs.rightLongitude, prefs.bottomLatitude, z, out etx, out ety);

                    if (etx < stx) etx += 1 << z;

                    if (etx - stx > tx && ety - sty > ty)
                    {
                        textureLevel = z;
                        break;
                    }
                }

                if (textureLevel == 0) textureLevel = 24;
            }
            else textureLevel = prefs.maxTextureLevel;

            maxTextureLevel = textureLevel;

            tiles = new Dictionary<ulong, RealWorldTerrainVectorTile>();

            int max = 1 << maxTextureLevel;

            double tlx, tly, brx, bry;
            RealWorldTerrainUtils.LatLongToTile(prefs.leftLongitude, prefs.topLatitude, maxTextureLevel, out tlx, out tly);
            RealWorldTerrainUtils.LatLongToTile(prefs.rightLongitude, prefs.bottomLatitude, maxTextureLevel, out brx, out bry);

            int itlx = (int) tlx;
            int itly = (int) tly;
            int ibrx = (int) Math.Ceiling(brx);
            int ibry = (int) Math.Ceiling(bry);

            if (itlx > ibrx) ibrx += max;

            for (int x = itlx; x < ibrx; x++)
            {
                int cx = x;
                if (cx >= max) cx -= max;

                for (int y = itly; y < ibry; y++)
                {
                    RealWorldTerrainVectorTile tile = new RealWorldTerrainVectorTile(cx, y, maxTextureLevel);
                    tile.Download();
                    tiles.Add(tile.key, tile);
                }
            }
        }

        public static bool IsPointInPolygon(Vector3[] poly, int length, float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = length - 1; i < length; j = i++)
            {
                if ((poly[i].z <= y && y < poly[j].z ||
                     poly[j].z <= y && y < poly[i].z) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }
#endif
    }
}