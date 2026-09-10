// 原生探针:以与 SDK 头文件完全一致的方式调用 TaskDialogIndirect。
// 目标:判定 comctl32 v6 对 TASKDIALOGCONFIG 布局与回调的真实行为。
#include <windows.h>
#include <commctrl.h>
#include <stdio.h>

static int g_ticks = 0;

static HRESULT CALLBACK Cb(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, LONG_PTR refData)
{
    if (msg == TDN_TIMER)
    {
        g_ticks++;
        fprintf(stderr, "[native] TDN_TIMER #%d elapsed=%zu\n", g_ticks, (size_t)wp);
        if (g_ticks == 2)
        {
            // 验证旧版 TDM_RETURN_VALUE(0x0231):期望对话框关闭且 btn == 0x7B
            SendMessage(hwnd, 0x0231, 0x7B, 0);
            fprintf(stderr, "[native] sent TDM_RETURN_VALUE(0x0231, wParam=0x7B)\n");
        }
    }
    return S_OK;
}

int main()
{
    INITCOMMONCONTROLSEX icc = { sizeof(icc), ICC_STANDARD_CLASSES };
    if (!InitCommonControlsEx(&icc))
        fprintf(stderr, "[native] InitCommonControlsEx failed\n");

    fprintf(stderr, "[native] sizeof(TASKDIALOGCONFIG)=%zu\n", sizeof(TASKDIALOGCONFIG));

    TASKDIALOGCONFIG cfg = { 0 };
    cfg.cbSize = sizeof(cfg);
    cfg.dwFlags = TDF_CALLBACK_TIMER;
    cfg.pszWindowTitle = L"native probe title-0xBEEF";
    cfg.pszMainInstruction = L"native instruction text";
    cfg.pfCallback = Cb;

    int btn = 0, radio = 0;
    BOOL ver = FALSE;
    HRESULT hr = TaskDialogIndirect(&cfg, &btn, &radio, &ver);
    fprintf(stderr, "[native] hr=0x%08X btn=%d radio=%d ver=%d ticks=%d\n", (unsigned)hr, btn, radio, ver, g_ticks);
    return 0;
}
