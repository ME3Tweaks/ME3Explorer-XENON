﻿//This class was generated by ME3Explorer
//Author: Warranty Voider
//URL: http://sourceforge.net/projects/me3explorer/
//URL: http://me3explorer.freeforums.org/
//URL: http://www.facebook.com/pages/Creating-new-end-for-Mass-Effect-3/145902408865659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using ME3Explorer.Unreal;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace ME3Explorer.Unreal.Classes
{
    public class BrushComponent
    {
        #region Unreal Props

        //Byte Properties

        public int RBChannel;
        //Bool Properties

        public bool BlockRigidBody = false;
        public bool CollideActors = false;
        public bool CanBlockCamera = false;
        public bool BlockZeroExtent = false;
        public bool BlockNonZeroExtent = false;
        public bool BlockActors = false;
        public bool bAcceptsDynamicDecals = false;
        public bool bCastDynamicShadow = false;
        public bool bAcceptsDynamicDominantLightShadows = false;
        public bool bAcceptsLights = false;
        public bool bAcceptsDynamicLights = false;
        public bool bAllowCullDistanceVolume = false;
        public bool bAcceptsFoliage = false;
        public bool bAllowAmbientOcclusion = false;
        public bool bAllowShadowFade = false;
        //Object Properties

        public int Brush;
        public int ReplacementPrimitive;
        public int PhysMaterialOverride;
        //Integer Properties

        public int CachedPhysBrushDataVersion;

        #endregion

        public int MyIndex;
        public PCCObject pcc;
        public byte[] data;
        public List<PropertyReader.Property> Props;

        public Vector3[] Vertices;
        public int[] Faces;
        public CustomVertex.PositionColored[] BrushMesh;
        public bool isSelected;

        public BrushComponent(PCCObject Pcc, int Index)
        {
            pcc = Pcc;
            MyIndex = Index;
            if (pcc.isExport(Index))
                data = pcc.Exports[Index].Data;
            Props = PropertyReader.getPropList(pcc, data);
            BitConverter.IsLittleEndian = true;
            foreach (PropertyReader.Property p in Props)
                switch (pcc.getNameEntry(p.Name))
                {

                    case "RBChannel":
                        RBChannel = p.Value.IntValue;
                        break;
                    case "BlockRigidBody":
                        if (p.raw[p.raw.Length - 1] == 1)
                            BlockRigidBody = true;
                        break;
                    case "CollideActors":
                        if (p.raw[p.raw.Length - 1] == 1)
                            CollideActors = true;
                        break;
                    case "CanBlockCamera":
                        if (p.raw[p.raw.Length - 1] == 1)
                            CanBlockCamera = true;
                        break;
                    case "BlockZeroExtent":
                        if (p.raw[p.raw.Length - 1] == 1)
                            BlockZeroExtent = true;
                        break;
                    case "BlockNonZeroExtent":
                        if (p.raw[p.raw.Length - 1] == 1)
                            BlockNonZeroExtent = true;
                        break;
                    case "BlockActors":
                        if (p.raw[p.raw.Length - 1] == 1)
                            BlockActors = true;
                        break;
                    case "bAcceptsDynamicDecals":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAcceptsDynamicDecals = true;
                        break;
                    case "bCastDynamicShadow":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bCastDynamicShadow = true;
                        break;
                    case "bAcceptsDynamicDominantLightShadows":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAcceptsDynamicDominantLightShadows = true;
                        break;
                    case "bAcceptsLights":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAcceptsLights = true;
                        break;
                    case "bAcceptsDynamicLights":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAcceptsDynamicLights = true;
                        break;
                    case "bAllowCullDistanceVolume":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAllowCullDistanceVolume = true;
                        break;
                    case "bAcceptsFoliage":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAcceptsFoliage = true;
                        break;
                    case "bAllowAmbientOcclusion":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAllowAmbientOcclusion = true;
                        break;
                    case "bAllowShadowFade":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAllowShadowFade = true;
                        break;
                    case "Brush":
                        Brush = p.Value.IntValue;
                        break;
                    case "ReplacementPrimitive":
                        ReplacementPrimitive = p.Value.IntValue;
                        break;
                    case "PhysMaterialOverride":
                        PhysMaterialOverride = p.Value.IntValue;
                        break;
                    case "CachedPhysBrushDataVersion":
                        CachedPhysBrushDataVersion = p.Value.IntValue;
                        break;
                    case "BrushAggGeom":
                        ReadMesh(p.raw);
                        break;
                        
                }
        }        

        public void ReadMesh(byte[] raw)
        {
            byte[] t1 = new byte[raw.Length - 32];
            for (int i = 0; i < raw.Length - 32; i++)
                t1[i] = raw[i + 32];
            int size1 = GetArraySize(t1);
            byte[] t2 = new byte[size1];
            for (int i = 0; i < size1; i++)
                t2[i] = t1[i + 28];
            List<PropertyReader.Property> pp = PropertyReader.ReadProp(pcc, t2, 0);
            foreach (PropertyReader.Property p in pp)
            {
                string name = pcc.getNameEntry(p.Name);
                switch (name)
                {
                    case "VertexData":
                        ReadVertices(p.raw);
                        break;
                    case "FaceTriData":
                        ReadFaces(p.raw);
                        break;
                }
            }
            if (Vertices != null && Faces != null)
            {
                BrushMesh = new CustomVertex.PositionColored[Faces.Length];
                for (int i = 0; i < Faces.Length; i++)
                    BrushMesh[i] = new CustomVertex.PositionColored(Vertices[Faces[i]], Color.Orange.ToArgb());
            }
        }

        public void ReadVertices(byte[] raw)
        {
            int count = GetArrayCount(raw);
            Vertices = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vertices[i].X = BitConverter.ToSingle(raw, 28 + i * 12);
                Vertices[i].Y = BitConverter.ToSingle(raw, 32 + i * 12);
                Vertices[i].Z = BitConverter.ToSingle(raw, 36 + i * 12);
            }
        }

        public void ReadFaces(byte[] raw)
        {
            int count = GetArrayCount(raw);
            Faces = new int[count];
            for (int i = 0; i < count; i++)
                Faces[i] = BitConverter.ToInt32(raw, 28 + i * 4);
        }

        public int GetArraySize(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 16); ;
        }

        public int GetArrayCount(byte[] raw)
        {
            return BitConverter.ToInt32(raw,24);
        }

        public void Render(Device device)
        {
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.RenderState.Lighting = false;
            if(isSelected)
                device.RenderState.FillMode = FillMode.Solid;
            else
                device.RenderState.FillMode = FillMode.WireFrame;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 12, BrushMesh);
        }

        public void SetSelection(bool Selected)
        {
            isSelected = Selected;
        }

        public TreeNode ToTree()
        {
            TreeNode res = new TreeNode("#" + MyIndex + " : " + pcc.Exports[MyIndex].ObjectName);
            res.Nodes.Add("RBChannel : " + pcc.getNameEntry(RBChannel));
            res.Nodes.Add("BlockRigidBody : " + BlockRigidBody);
            res.Nodes.Add("CollideActors : " + CollideActors);
            res.Nodes.Add("CanBlockCamera : " + CanBlockCamera);
            res.Nodes.Add("BlockZeroExtent : " + BlockZeroExtent);
            res.Nodes.Add("BlockNonZeroExtent : " + BlockNonZeroExtent);
            res.Nodes.Add("BlockActors : " + BlockActors);
            res.Nodes.Add("bAcceptsDynamicDecals : " + bAcceptsDynamicDecals);
            res.Nodes.Add("bCastDynamicShadow : " + bCastDynamicShadow);
            res.Nodes.Add("bAcceptsDynamicDominantLightShadows : " + bAcceptsDynamicDominantLightShadows);
            res.Nodes.Add("bAcceptsLights : " + bAcceptsLights);
            res.Nodes.Add("bAcceptsDynamicLights : " + bAcceptsDynamicLights);
            res.Nodes.Add("bAllowCullDistanceVolume : " + bAllowCullDistanceVolume);
            res.Nodes.Add("bAcceptsFoliage : " + bAcceptsFoliage);
            res.Nodes.Add("bAllowAmbientOcclusion : " + bAllowAmbientOcclusion);
            res.Nodes.Add("bAllowShadowFade : " + bAllowShadowFade);
            res.Nodes.Add("Brush : " + Brush);
            res.Nodes.Add("ReplacementPrimitive : " + ReplacementPrimitive);
            res.Nodes.Add("PhysMaterialOverride : " + PhysMaterialOverride);
            res.Nodes.Add("CachedPhysBrushDataVersion : " + CachedPhysBrushDataVersion);
            return res;
        }

    }
}