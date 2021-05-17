using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class LimitedColorRenderPass : ScriptableRenderPass {
    string profilerTag;

    Material materialToBlit;
    Color[] colors;
    RenderTargetIdentifier cameraColorTargetIdent;
    RenderTargetHandle tempTexture;

    public LimitedColorRenderPass (string profilerTag, RenderPassEvent renderPassEvent, Material materialToBlit, Color[] colors) {
        this.profilerTag = profilerTag;
        this.renderPassEvent = renderPassEvent;
        this.materialToBlit = materialToBlit;
        this.colors = colors;
    }
    
    public void Setup (RenderTargetIdentifier cameraColorTargetIdent) {
        this.cameraColorTargetIdent = cameraColorTargetIdent;
    }
    
    public override void Configure (CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
        cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
    }
    
    public override void Execute (ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
        cmd.Clear();

        materialToBlit.SetInt("_ColorCount", colors.Length);
        materialToBlit.SetColorArray("_Colors", colors);

        cmd.Blit(cameraColorTargetIdent, tempTexture.Identifier(), materialToBlit, 0);
        cmd.Blit(tempTexture.Identifier(), cameraColorTargetIdent);
        
        context.ExecuteCommandBuffer(cmd);
        
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
    
    public override void FrameCleanup (CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(tempTexture.id);
    }
}