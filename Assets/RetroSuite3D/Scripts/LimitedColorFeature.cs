using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LimitedColorFeature : ScriptableRendererFeature {
    [System.Serializable]
    public class MyFeatureSettings {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material limitedColorMaterial;

        public Color[] palette;
    }

    // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
    public MyFeatureSettings settings = new MyFeatureSettings();

    RenderTargetHandle renderTextureHandle;
    LimitedColorRenderPass myRenderPass;

    public override void Create () {
        myRenderPass = new LimitedColorRenderPass(
          "LimitedColorEffect",
          settings.WhenToInsert,
          settings.limitedColorMaterial,
          settings.palette
        );
    }
    
    public override void AddRenderPasses (ScriptableRenderer renderer, ref RenderingData renderingData) {
        if(!settings.IsEnabled) {
            return;
        }
        
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        myRenderPass.Setup(cameraColorTargetIdent);
        
        renderer.EnqueuePass(myRenderPass);
    }
}