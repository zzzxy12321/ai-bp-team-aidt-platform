using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Infrastructure.Configuration;

namespace AIDT.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class PlatformCapabilityService(AidtRuntimeOptions options) : IPlatformCapabilityService
{
    public PlatformCapabilitiesDto GetCapabilities()
    {
        var databaseState = options.Database.IsReady ? "available" : "blocked";
        var workflowState = options.Workflow.IsConfigured ? "available" : "planned";
        var capabilities = new List<CapabilityDto>
        {
            new("PLT-001", "本地测试身份", databaseState, "Cookie/CSRF、RBAC、部门和字段范围已验证；该身份不是生产 SSO。"),
            new("UX-001", "员工优先入口", "available", "我的工作、员工导航、管理入口分层和桌面/390px E2E 已验证。"),
            new("UX-003", "统一工作事项中心", databaseState, DatabaseReason("流程任务、项目任务、事件、听记行动项、MCP 确认与 Agent 确认统一投影到我的工作，并保留原详情权限。")),
            new("UX-004", "业务可编辑的人工确认中心", databaseState, DatabaseReason("事件、听记、MCP 与 Agent 四类人工确认已统一入口；支持可编辑负责人、期限、对象、项目和流程，并保留业务影响、技术折叠与审计。")),
            new("WS-001", "部门工作区", databaseState, "部门成员、主部门、部门范围和跨部门任务流转已验证。"),
            new("WS-002", "可配置岗位工作台模板", databaseState, DatabaseReason("研发、质量、供应链、财务、经理和项目经理模板已服务端匹配；卡片复用真实权限，草稿/发布/停用/恢复和无匹配回退已验证。")),
            new("DOC-001", "部门文件与附件中心", databaseState, "上传、下载、预览、版本、敏感文件门禁和审计已验证。"),
            new("DATA-001", "演示与真实数据隔离", "available", "对象、流程和文件环境过滤、管理员跨环境开关已验证。"),
            new("MIN-001", "AI 听记与行动项", "available", "支持 DOCX/TXT/MD/JSON 文字稿、规则模拟提取、人工确认、任务追溯和部门/环境隔离；PDF、钉钉与真实模型未接入。"),
            new("WF-005", "流程节点能力包与 SLA", databaseState, "策略管理页支持草稿/发布与版本冻结，SLA、提醒、升级和默认演示策略已验证。"),
            new("AGT-002", "超时升级与受限代理", databaseState, "支持人工确认代理和 SLA 预授权自动代理；独立服务身份、幂等键、通知和永久人工边界已验证。"),
            new("AIG-002", "AgentRun 与 AiGateway 关联", "available", "人工确认后的 AgentRun 调用 RuleSimulator 并保存 GatewayCall；ExternalModelCalled=false。"),
            new("PMO-001", "项目主对象与生命周期", databaseState, DatabaseReason("PROJECT 主对象、PMO 项目档案、项目状态和成员/里程碑已使用 SQL Server 持久化。")),
            new("PMO-002", "项目成员、里程碑与项目工作台", databaseState, DatabaseReason("项目成员、主负责人、里程碑、任务进度、SLA 风险和证据时间线已纳入。")),
            new("PMO-003", "项目操作闭环", databaseState, DatabaseReason("项目编辑、负责人更换、成员管理、任务创建、证据关联、进度重算和状态流转已持久化。")),
            new("EVT-001", "企业事件收集收件箱", databaseState, DatabaseReason("支持手动事项、TXT/MD/JSON/DOCX、听记、行动项、通知和附件来源；真实外部监听未启用。")),
            new("TASK-002", "事件行动项转项目任务", databaseState, DatabaseReason("Agent RuleSimulator 仅生成建议，真人确认后幂等创建项目任务并保留来源和审计。")),
            new("META-001", "业务对象建模与字段编辑", databaseState, DatabaseReason("对象类型草稿、校验、发布、停用、恢复、版本历史、九类字段与业务对象安全编辑已接入。")),
            new("OBJ-001", "业务对象定义", databaseState, DatabaseReason("对象类型和字段可由授权管理员通过 API 维护，不依赖源码或重跑迁移。")),
            new("OBJ-002", "业务对象实例", databaseState, DatabaseReason("业务对象创建与编辑已持久化，并校验字段类型、必填、长度、选项、版本和审计。")),
            new("WF-001", "客户样品到转产", workflowState, WorkflowReason("客户样品流程")),
            new("WF-002", "批次异常到 CAPA", workflowState, WorkflowReason("批次异常流程")),
            new("WF-003", "请购到付款", workflowState, WorkflowReason("请购付款流程")),
            new("WF-004", "客诉到 CAPA", workflowState, WorkflowReason("客诉流程")),
            new("TASK-001", "流程待办", databaseState, DatabaseReason()),
            new("AUD-001", "审计与证据", databaseState, DatabaseReason("审计与业务事务同步写入 SQL Server。")),
            new("AGT-001", "数字员工运行", "contract_ready", "人工确认、停止和权限边界已验证；AiGateway 使用规则模拟，不调用真实模型。"),
            new("INT-001", "外部连接器", "disconnected", "仅保留 Outbox/回执契约，未连接外部系统。"),
            new("CFG-001", "业务流程配置版本与发布", databaseState, DatabaseReason("支持配置草稿、Revision 并发控制、结构校验、发布、可执行 WorkflowVersion 联动和回滚。")),
            new("NAV-001", "可配置组织栏目与工作区导航", databaseState, DatabaseReason("导航版本、默认模板、部门范围、权限过滤、停用恢复和个人偏好已接入；系统入口不可删除。")),
            new("IAM-001", "组织、人员、角色和权限配置中心", databaseState, DatabaseReason("组织树、邀请开通、数据范围、角色权限矩阵、自提权阻断和双人审核接缝已接入。")),
            new("WFROLE-001", "流程业务角色与节点级授权", databaseState, DatabaseReason("六类处理人策略、任务解析快照、节点资格、真实项目经理审批和转交升级已接入。")),
            new("MCP-001", "AIDT 标准 MCP 服务", databaseState, DatabaseReason("官方 .NET MCP SDK 2.3.0；OAuth 2.1/PKCE、Streamable HTTP、stdio、工具/Resources、真人确认和调用审计已接入。")),
            new("DEP-001", "离线部署", "contract_ready", "测试部署、应用回滚和恢复点已验证；离线依赖验证状态以构建证据为准。")
        };

        return new PlatformCapabilitiesDto(
            options.ProjectState,
            options.Database.MigrationState,
            options.Database.Provider,
            options.ExternalSystems.Mode,
            options.Data.CurrentEnvironmentCode,
            options.FileStorage.MaxSizeBytes,
            capabilities);
    }

    private string DatabaseReason(string? readyReason = null) =>
        options.Database.IsReady
            ? readyReason ?? "SQL Server 连接、迁移和持久化已通过验证。"
            : "阶段 A 仅交付契约与迁移源文件；本轮不连接 SQL Server、不执行迁移。";

    private string WorkflowReason(string workflowName) =>
        options.Workflow.IsConfigured
            ? $"{workflowName}运行时已配置；实际持久化结果以集成证据为准。"
            : $"{workflowName}仅保留 Elsa 接缝；阶段 A 不启动流程运行时。";
}

public interface IPersistenceGate
{
    void EnsureAvailable(string feature);
}

public sealed class PersistenceGate(AidtRuntimeOptions options) : IPersistenceGate
{
    public void EnsureAvailable(string feature)
    {
        if (!options.Database.IsReady)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.PersistenceNotReady,
                $"{feature} 需要 SQL Server 持久化；当前为 SQL BLOCKED。");
        }
    }
}
