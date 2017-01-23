// Decompiled with JetBrains decompiler
// Type: SynapseGaming.DRM.ApertureClient.Test.ApertureAuthorityService
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Protocols;

namespace SynapseGaming.DRM.ApertureClient.Test
{
  /// <remarks />
  [WebServiceBinding(Name = "ApertureAuthorityServiceSoap", Namespace = "https://aperture.synapsegaming.net/")]
  [DebuggerStepThrough]
  [DesignerCategory("code")]
  public class ApertureAuthorityService : SoapHttpClientProtocol
  {
    private SendOrPostCallback sendOrPostCallback_0;
    private SendOrPostCallback sendOrPostCallback_1;
    private VerifyAuthorizationCodeCompletedEventHandler verifyAuthorizationCodeCompletedEventHandler_0;
    private GenerateAuthorizationFileCompletedEventHandler generateAuthorizationFileCompletedEventHandler_0;

    /// <remarks />
    public ApertureAuthorityService()
    {
      this.Url = "https://192.168.0.70/webservices/ApertureAuthority.asmx";
    }

    [SpecialName]
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void method_0(VerifyAuthorizationCodeCompletedEventHandler verifyAuthorizationCodeCompletedEventHandler_1)
    {
      this.verifyAuthorizationCodeCompletedEventHandler_0 = this.verifyAuthorizationCodeCompletedEventHandler_0 + verifyAuthorizationCodeCompletedEventHandler_1;
    }

    [SpecialName]
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void method_1(VerifyAuthorizationCodeCompletedEventHandler verifyAuthorizationCodeCompletedEventHandler_1)
    {
      this.verifyAuthorizationCodeCompletedEventHandler_0 = this.verifyAuthorizationCodeCompletedEventHandler_0 - verifyAuthorizationCodeCompletedEventHandler_1;
    }

    [SpecialName]
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void method_2(GenerateAuthorizationFileCompletedEventHandler generateAuthorizationFileCompletedEventHandler_1)
    {
      this.generateAuthorizationFileCompletedEventHandler_0 = this.generateAuthorizationFileCompletedEventHandler_0 + generateAuthorizationFileCompletedEventHandler_1;
    }

    [SpecialName]
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void method_3(GenerateAuthorizationFileCompletedEventHandler generateAuthorizationFileCompletedEventHandler_1)
    {
      this.generateAuthorizationFileCompletedEventHandler_0 = this.generateAuthorizationFileCompletedEventHandler_0 - generateAuthorizationFileCompletedEventHandler_1;
    }

    /// <remarks />
    [SoapDocumentMethod("https://aperture.synapsegaming.net/VerifyAuthorizationCode", ParameterStyle = SoapParameterStyle.Wrapped, RequestNamespace = "https://aperture.synapsegaming.net/", ResponseNamespace = "https://aperture.synapsegaming.net/", Use = SoapBindingUse.Literal)]
    public bool VerifyAuthorizationCode(string encdata)
    {
      return (bool) this.Invoke("VerifyAuthorizationCode", new object[1]{ (object) encdata })[0];
    }

    /// <remarks />
    public IAsyncResult BeginVerifyAuthorizationCode(string encdata, AsyncCallback callback, object asyncState)
    {
      return this.BeginInvoke("VerifyAuthorizationCode", new object[1]{ (object) encdata }, callback, asyncState);
    }

    /// <remarks />
    public bool EndVerifyAuthorizationCode(IAsyncResult asyncResult)
    {
      return (bool) this.EndInvoke(asyncResult)[0];
    }

    /// <remarks />
    public void VerifyAuthorizationCodeAsync(string encdata)
    {
      this.VerifyAuthorizationCodeAsync(encdata, (object) null);
    }

    /// <remarks />
    public void VerifyAuthorizationCodeAsync(string encdata, object userState)
    {
      if (this.sendOrPostCallback_0 == null)
        this.sendOrPostCallback_0 = new SendOrPostCallback(this.method_4);
      this.InvokeAsync("VerifyAuthorizationCode", new object[1]
      {
        (object) encdata
      }, this.sendOrPostCallback_0, userState);
    }

    private void method_4(object object_0)
    {
      if (this.verifyAuthorizationCodeCompletedEventHandler_0 == null)
        return;
      InvokeCompletedEventArgs completedEventArgs = (InvokeCompletedEventArgs) object_0;
      this.verifyAuthorizationCodeCompletedEventHandler_0((object) this, new VerifyAuthorizationCodeCompletedEventArgs(completedEventArgs.Results, completedEventArgs.Error, completedEventArgs.Cancelled, completedEventArgs.UserState));
    }

    /// <remarks />
    [SoapDocumentMethod("https://aperture.synapsegaming.net/GenerateAuthorizationFile", ParameterStyle = SoapParameterStyle.Wrapped, RequestNamespace = "https://aperture.synapsegaming.net/", ResponseNamespace = "https://aperture.synapsegaming.net/", Use = SoapBindingUse.Literal)]
    public byte[] GenerateAuthorizationFile(string encdata)
    {
      return (byte[]) this.Invoke("GenerateAuthorizationFile", new object[1]{ (object) encdata })[0];
    }

    /// <remarks />
    public IAsyncResult BeginGenerateAuthorizationFile(string encdata, AsyncCallback callback, object asyncState)
    {
      return this.BeginInvoke("GenerateAuthorizationFile", new object[1]{ (object) encdata }, callback, asyncState);
    }

    /// <remarks />
    public byte[] EndGenerateAuthorizationFile(IAsyncResult asyncResult)
    {
      return (byte[]) this.EndInvoke(asyncResult)[0];
    }

    /// <remarks />
    public void GenerateAuthorizationFileAsync(string encdata)
    {
      this.GenerateAuthorizationFileAsync(encdata, (object) null);
    }

    /// <remarks />
    public void GenerateAuthorizationFileAsync(string encdata, object userState)
    {
      if (this.sendOrPostCallback_1 == null)
        this.sendOrPostCallback_1 = new SendOrPostCallback(this.method_5);
      this.InvokeAsync("GenerateAuthorizationFile", new object[1]
      {
        (object) encdata
      }, this.sendOrPostCallback_1, userState);
    }

    private void method_5(object object_0)
    {
      if (this.generateAuthorizationFileCompletedEventHandler_0 == null)
        return;
      InvokeCompletedEventArgs completedEventArgs = (InvokeCompletedEventArgs) object_0;
      this.generateAuthorizationFileCompletedEventHandler_0((object) this, new GenerateAuthorizationFileCompletedEventArgs(completedEventArgs.Results, completedEventArgs.Error, completedEventArgs.Cancelled, completedEventArgs.UserState));
    }
  }
}
