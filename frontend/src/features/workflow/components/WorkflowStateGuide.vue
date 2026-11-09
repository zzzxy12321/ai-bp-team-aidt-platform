<script setup lang="ts">
const states = [
  { code: 'Running', label: '运行中', detail: '等待或处理人工任务' },
  { code: 'Returned', label: '已退回', detail: '必须记录退回原因' },
  { code: 'Terminated', label: '已终止', detail: '保留操作者与审计' },
  { code: 'Completed', label: '已完成', detail: '流程版本固定不变' },
]
</script>

<template>
  <div class="state-guide">
    <article v-for="state in states" :key="state.code">
      <span>{{ state.code }}</span>
      <strong>{{ state.label }}</strong>
      <p>{{ state.detail }}</p>
    </article>
  </div>
</template>

<style scoped>
.state-guide {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
  margin-top: 18px;
}

article {
  border-radius: 12px;
  background: var(--aidt-brand-soft);
  padding: 14px;
}

span,
strong {
  display: block;
}

span {
  color: var(--aidt-brand);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.68rem;
}

strong {
  margin-top: 5px;
}

p {
  margin: 5px 0 0;
  font-size: 0.76rem;
}

@media (max-width: 720px) {
  .state-guide {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 430px) {
  .state-guide {
    grid-template-columns: 1fr;
  }
}
</style>
