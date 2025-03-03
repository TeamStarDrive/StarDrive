﻿// Decompiled with JetBrains decompiler
// Type: ns3.GraphicsDeviceMonitor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework.Graphics;

namespace ns3
{
    internal class GraphicsDeviceMonitor
    {
        private bool bool_0;
        private IGraphicsDeviceService igraphicsDeviceService_0;
        private int int_0;
        private int int_1;
        private MultiSampleType multiSampleType_0;
        private int int_2;

        public bool Changed
        {
            get
            {
                PresentationParameters presentationParameters = this.igraphicsDeviceService_0.GraphicsDevice.PresentationParameters;
                if (!this.bool_0 && presentationParameters.BackBufferWidth == this.int_0 && (presentationParameters.BackBufferHeight == this.int_1 && presentationParameters.MultiSampleType == this.multiSampleType_0) && presentationParameters.MultiSampleQuality == this.int_2)
                    return false;
                this.int_0 = presentationParameters.BackBufferWidth;
                this.int_1 = presentationParameters.BackBufferHeight;
                this.multiSampleType_0 = presentationParameters.MultiSampleType;
                this.int_2 = presentationParameters.MultiSampleQuality;
                this.bool_0 = false;
                return true;
            }
        }

        public GraphicsDeviceMonitor(IGraphicsDeviceService graphicsdevicemanager)
        {
            this.igraphicsDeviceService_0 = graphicsdevicemanager;
            this.igraphicsDeviceService_0.DeviceCreated += this.igraphicsDeviceService_0_DeviceDisposing;
            this.igraphicsDeviceService_0.DeviceReset += this.igraphicsDeviceService_0_DeviceDisposing;
            this.igraphicsDeviceService_0.DeviceDisposing += this.igraphicsDeviceService_0_DeviceDisposing;
        }

        private void igraphicsDeviceService_0_DeviceDisposing(object sender, EventArgs e)
        {
            this.bool_0 = true;
        }
    }
}
