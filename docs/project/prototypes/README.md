# AIDT 业务用户界面原型

## 📌 文件

- `AIDT_BUSINESS_UX_PROTOTYPE.html`

## 🖥️ 页面

1. 我的工作：员工发起事项、待办、最近事项和数字员工建议。
2. 项目工作区：以 `3157C` 为例展示里程碑、任务、附件、证据和审计。
3. 流程配置：使用业务语言配置流程节点、人员、附件、SLA 和数字员工。
4. 平台治理：配置发布、影响范围、版本和一致性告警。

## 🎯 设计原则

- 普通员工先看到事情，不先看到流程实例、业务对象 UUID 或 TraceId。
- 岗位差异通过视图、字段、权限和流程模板体现，不建设割裂数据库。
- 配置只组合已注册标准能力，不通过前端生成后端代码。
- 数字员工默认只生成建议，高风险动作永久保留人工确认。
- 流程配置必须经过草稿、沙箱、审核、发布、版本冻结和回滚。

## 🚫 边界

- 本原型不连接真实接口。
- 本原型不修改当前正式前端。
- 原型中的数据和操作仅用于设计评审。

## ▶️ 本地预览

在 `docs/project/prototypes` 目录运行：

```powershell
py -m http.server 8090 --bind 127.0.0.1
```

浏览器访问：

```text
http://127.0.0.1:8090/AIDT_BUSINESS_UX_PROTOTYPE.html
```

## 🖼️ 视觉验证

- `docs/project/test-artifacts/ui-prototype-employee.png`
- `docs/project/test-artifacts/ui-prototype-project.png`
- `docs/project/test-artifacts/ui-prototype-config.png`
- `docs/project/test-artifacts/ui-prototype-governance.png`
- `docs/project/test-artifacts/ui-prototype-request-modal.png`
- `docs/project/test-artifacts/ui-prototype-mobile-employee.png`

已验证：

- 四个页面可以切换。
- “发起事项”弹窗可以打开和关闭。
- 流程节点可以选中并切换配置焦点。
- 窄屏下导航、指标卡和发起入口保持可用。
- 浏览器控制台无 warning/error。
