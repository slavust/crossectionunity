﻿using System.Collections.Generic;
using UnityEngine;


public struct CrossSectionInfo
{
    public Vector3 m_position;
    public Vector3 m_normal;
}

public class BadContour
{
    public BadContour()
    {
        m_points = new List<Vector3>();
        m_bounding_sphere = new BoundingSphere();
        m_bounding_sphere_needs_update = false;
        m_stub_mesh = null;
    }
    public BadContour(List<Vector3> points)
    {
        m_points = points;
        m_bounding_sphere = new BoundingSphere();
        m_bounding_sphere_needs_update = true;
        m_stub_mesh = null;
    }

    public int PointCount
    {
        get
        {
            return m_points.Count;
        }
    }

    public Vector3 GetPoint(int indx)
    {
        return m_points[indx];
    }

    public void AddNextPoint(Vector3 pt)
    {
        m_bounding_sphere_needs_update = true;
        m_stub_mesh = null;
        m_points.Add(pt);
    }

    public Vector3[] PointsArray
    {
        get { return m_points.ToArray(); }
    }

    public List<Vector3> Points
    {
        get { return m_points; }
    }

    public Mesh GenerateStubMesh()
    {
        if (m_stub_mesh)
            return m_stub_mesh;

        Vector3 center = new Vector3(0, 0, 0);
        for (int i = 0; i < this.PointCount; ++i)
            center += this.GetPoint(i);
        center /= this.PointCount;

        List<Vector3> stub_vertices = new List<Vector3>(this.Points);
        stub_vertices.Add(center);

        List<int> stub_indices = new List<int>();
        for (int i = 0, j = i + 1; i < this.PointCount; ++i, ++j)
        {
            int index_1 = i;
            int index_2 = j % this.PointCount;
            int index_3 = stub_vertices.Count - 1;

            stub_indices.Add(index_1);
            stub_indices.Add(index_2);
            stub_indices.Add(index_3);
        }

        Mesh stub_mesh = new Mesh();
        stub_mesh.SetVertices(stub_vertices);
        stub_mesh.SetIndices(stub_indices.ToArray(), MeshTopology.Triangles, 0);

        m_stub_mesh = stub_mesh;
        return m_stub_mesh;
    }

    public BadContour GenerateFrameBadContour(List<CrossSectionInfo> model_cross_sections)
    {
        return this;
    }

    private bool BoundingSphereIntersectsCrossSection(CrossSectionInfo cross_section)
    {
        UpdateBoundingSphere();
        Vector3 plane_pos_to_sphere_center = m_bounding_sphere.position - cross_section.m_position;
        var distance_to_plane = Mathf.Abs(Vector3.Dot(plane_pos_to_sphere_center, cross_section.m_normal)); // todo: check?
        return distance_to_plane <= m_bounding_sphere.radius;
    }

    private List<bool> CalculateVisibilityForPointList(List<Vector3> points, CrossSectionInfo cross_section)
    {
        List<bool> visibility = new List<bool>();
        for (int i = 0; i < points.Count; ++i)
        {
            bool point_visible = CalculateVisibilityForPoint(points[i], cross_section);
            visibility.Add(point_visible);
        }
        return visibility;
    }

    private bool CalculateVisibilityForPoint(Vector3 point, CrossSectionInfo cross_section)
    {
        Vector3 plane_pos_to_point = Vector3.Normalize(point - cross_section.m_position);
        var dot = Vector3.Dot(plane_pos_to_point, cross_section.m_normal);
        return dot <= 0.0f;
    }

    private Vector3 CalculateEdgeWithPlaneIntersection(
        Vector3 edge_p1,
        Vector3 edge_p2,
        Vector3 plane_p,
        Vector3 plane_normal)
    {
        Vector3 line_dir = edge_p1 - edge_p2;
        float LdotN = Vector3.Dot(line_dir, plane_normal);
        if (LdotN == 0.0f)
            return (edge_p1 + edge_p1) * 0.5f;
        float distance = Vector3.Dot(plane_p - edge_p1, plane_normal) / LdotN;
        Vector3 position = edge_p1 + line_dir * distance;
        return position;
    }

    private void UpdateBoundingSphere()
    {
        if (!m_bounding_sphere_needs_update)
            return;
        Vector3 center = new Vector3(0, 0, 0);
        foreach (var pt in m_points)
            center += pt;
        center /= m_points.Count;

        float max_distance_from_center = 0.0f;
        foreach (var pt in m_points)
        {
            float distance = Vector3.Distance(pt, center);
            if (distance > max_distance_from_center)
                max_distance_from_center = distance;
        }

        m_bounding_sphere.position = center;
        m_bounding_sphere.radius = max_distance_from_center;
        m_bounding_sphere_needs_update = false;
    }

    private List<Vector3> m_points;
    private bool m_bounding_sphere_needs_update;
    private BoundingSphere m_bounding_sphere;
    private Mesh m_stub_mesh = null;
}
