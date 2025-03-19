using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class CameraLayoutExporter
{
    public static void Export(string fileName, List<Vector3> cameraPositions, Vector3 centerPosition)
    {
        // Build the full path using Application.dataPath (Assets folder in Editor)
        string fullPath = Application.dataPath + "/" + fileName;
        
        if (cameraPositions == null || cameraPositions.Count == 0)
        {
            Debug.LogWarning("No camera positions to export.");
            return;
        }

        // Determine bounding box in XZ plane.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        List<Vector3> allPoints = new List<Vector3>();
        allPoints.Add(centerPosition);
        allPoints.AddRange(cameraPositions);

        foreach (Vector3 p in allPoints)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }
        if (Mathf.Approximately(minX, maxX)) { minX -= 1f; maxX += 1f; }
        if (Mathf.Approximately(minZ, maxZ)) { minZ -= 1f; maxZ += 1f; }

        int gridWidth = 60, gridHeight = 20;
        char[,] grid = new char[gridHeight, gridWidth];
        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
            {
                grid[r, c] = ' ';
            }
        }

        // Mapping from world coordinates (X,Z) to grid cell.
        Func<float, float, (int col, int row)> MapToGrid = (wx, wz) =>
        {
            float nx = (wx - minX) / (maxX - minX);
            float nz = (wz - minZ) / (maxZ - minZ);
            int col = Mathf.Clamp(Mathf.FloorToInt(nx * (gridWidth - 1)), 0, gridWidth - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt((1f - nz) * (gridHeight - 1)), 0, gridHeight - 1);
            return (col, row);
        };

        // Place center marker.
        var centerCell = MapToGrid(centerPosition.x, centerPosition.z);
        grid[centerCell.row, centerCell.col] = '^';

        // Place each camera using its number (first digit).
        for (int i = 0; i < cameraPositions.Count; i++)
        {
            var cell = MapToGrid(cameraPositions[i].x, cameraPositions[i].z);
            grid[cell.row, cell.col] = (i + 1).ToString()[0];
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("CAMERA LAYOUT (ASCII Top-Down View)");
        sb.AppendLine($"Bounding Box: X[{minX:F1}, {maxX:F1}]  Z[{minZ:F1}, {maxZ:F1}]");
        sb.AppendLine();
        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
                sb.Append(grid[r, c]);
            sb.AppendLine();
        }
        sb.AppendLine();

        // Append camera data table.
        sb.AppendLine("Camera Data Table:");
        sb.AppendLine(" Camera |   Angle   |  Dist  | Height ");
        sb.AppendLine("---------------------------------------");
        for (int i = 0; i < cameraPositions.Count; i++)
        {
            Vector3 offset = cameraPositions[i] - centerPosition;
            float angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            float distance = new Vector2(offset.x, offset.z).magnitude;
            float height = offset.y;
            sb.AppendLine($"   C{i+1}   | {angle,7:F1}° | {distance,6:F2} | {height,6:F2}");
        }

        try
        {
            File.WriteAllText(fullPath, sb.ToString());
            Debug.Log("Exported camera layout to file: " + fullPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to export camera layout: " + ex.Message);
        }
    }
}
