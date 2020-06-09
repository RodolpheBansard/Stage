using System;
using UnityEngine;

public class ElevationFromMapboxRGB_OM3:MonoBehaviour
{
    public Texture2D mapboxRGBElevation;

    public float topLatitude;
    public float leftLongitude;

    public float bottomLatitude;
    public float rightLongitude;

    private OnlineMapsTileSetControl control;

    private short[,] elevations;
    private int width;
    private int height;

    private void OnGetElevation(double left, double top, double right, double bottom)
    {
        short[,] elevation = new short[32, 32];

        double tlx, tly, brx, bry;
        double ttlx, ttly, tbrx, tbry;
        int zoom = 20;
        OnlineMaps.instance.projection.CoordinatesToTile(left, top, zoom, out tlx, out tly);
        OnlineMaps.instance.projection.CoordinatesToTile(right, bottom, zoom, out brx, out bry);

        OnlineMaps.instance.projection.CoordinatesToTile(leftLongitude, topLatitude, zoom, out ttlx, out ttly);
        OnlineMaps.instance.projection.CoordinatesToTile(rightLongitude, bottomLatitude, zoom, out tbrx, out tbry);

        double rangeX = tbrx - ttlx;
        double rangeY = tbry - ttly;
        double sx = (tlx - ttlx) / rangeX;
        double sy = (tly - ttly) / rangeY;
        double ex = (brx - ttlx) / rangeX;
        double ey = (bry - ttly) / rangeY;

        float step = 1 / 31f;

        for (int z = 0; z < 32; z++)
        {
            float lz = 1 - step * z;
            float cz = (float)((ey - sy) * lz + sy) * height;
            int iz = (int) cz;
            if (iz < 0 || iz >= height - 1) continue;

            float oz = cz - iz;

            for (int x = 0; x < 32; x++)
            {
                float lx = step * x;
                float cx = (float)((ex - sx) * lx + sx) * width;
                int ix = (int) cx;

                if (ix < 0 || ix >= width - 1) continue;

                float ox = cx - ix;

                float e1 = elevations[ix, iz];
                float e2 = elevations[ix + 1, iz];
                float e3 = elevations[ix, iz + 1];
                float e4 = elevations[ix + 1, iz + 1];

                e1 = (e2 - e1) * ox + e1;
                e3 = (e4 - e3) * ox + e3;
                e1 = (e3 - e1) * oz + e1;

                elevation[x, z] = (short) Mathf.FloorToInt(e1);
            }
        }

        if (OnlineMapsBingMapsElevationManager.instance != null) OnlineMapsBingMapsElevationManager.instance.SetElevationData(elevation);
    }

    private void Start()
    {
        try
        {
            TryLoadElevations();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + "\n" + e.StackTrace);
            Debug.LogError("Please make sure that the texture has Read / Write Enabled - ON.");
            return;
        }

        // Get Tileset control.
        control = OnlineMapsTileSetControl.instance;

        OnlineMaps.instance.SetPosition((leftLongitude + rightLongitude) / 2, (topLatitude + bottomLatitude) / 2);
        OnlineMaps.instance.zoom = 8;

        if (control == null)
        {
            Debug.LogError("You must use the Tileset control.");
            return;
        }

        // Intercept elevation request
        if (OnlineMapsBingMapsElevationManager.instance) OnlineMapsElevationManagerBase.instance.OnGetElevation += OnGetElevation;
    }

    private void TryLoadElevations()
    {
        Color[] colors = mapboxRGBElevation.GetPixels();
        width = mapboxRGBElevation.width;
        height = mapboxRGBElevation.height;
        elevations = new short[width, height];

        for (int y = 0; y < height; y++)
        {
            int py = (height - y - 1) * width;

            for (int x = 0; x < width; x++)
            {
                Color c = colors[py + x];

                double h = -10000 + (c.r * 255 * 256 * 256 + c.g * 255 * 256 + c.b * 255) * 0.1;
                elevations[x, y] = (short)Math.Round(h);
            }
        }
    }
}