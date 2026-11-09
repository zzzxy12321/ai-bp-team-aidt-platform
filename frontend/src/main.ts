import { createApp } from 'vue'
import {
  ElAlert,
  ElButton,
  ElCheckbox,
  ElCheckboxGroup,
  ElDatePicker,
  ElDialog,
  ElDropdown,
  ElDropdownItem,
  ElDropdownMenu,
  ElEmpty,
  ElForm,
  ElFormItem,
  ElInput,
  ElInputNumber,
  ElLoading,
  ElOption,
  ElPagination,
  ElProgress,
  ElRadio,
  ElRadioButton,
  ElRadioGroup,
  ElSelect,
  ElSkeleton,
  ElSwitch,
  ElTable,
  ElTableColumn,
  ElTabPane,
  ElTabs,
  ElTag,
  ElTimeline,
  ElTimelineItem,
  ElTree,
} from 'element-plus'
import 'element-plus/dist/index.css'
import './styles/tokens.css'
import './style.css'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(ElLoading)
app.component('ElAlert', ElAlert)
app.component('ElButton', ElButton)
app.component('ElCheckbox', ElCheckbox)
app.component('ElCheckboxGroup', ElCheckboxGroup)
app.component('ElDatePicker', ElDatePicker)
app.component('ElDialog', ElDialog)
app.component('ElDropdown', ElDropdown)
app.component('ElDropdownItem', ElDropdownItem)
app.component('ElDropdownMenu', ElDropdownMenu)
app.component('ElEmpty', ElEmpty)
app.component('ElForm', ElForm)
app.component('ElFormItem', ElFormItem)
app.component('ElInput', ElInput)
app.component('ElInputNumber', ElInputNumber)
app.component('ElOption', ElOption)
app.component('ElPagination', ElPagination)
app.component('ElProgress', ElProgress)
app.component('ElRadio', ElRadio)
app.component('ElRadioButton', ElRadioButton)
app.component('ElRadioGroup', ElRadioGroup)
app.component('ElSelect', ElSelect)
app.component('ElSkeleton', ElSkeleton)
app.component('ElSwitch', ElSwitch)
app.component('ElTable', ElTable)
app.component('ElTableColumn', ElTableColumn)
app.component('ElTabPane', ElTabPane)
app.component('ElTabs', ElTabs)
app.component('ElTag', ElTag)
app.component('ElTimeline', ElTimeline)
app.component('ElTimelineItem', ElTimelineItem)
app.component('ElTree', ElTree)
app.mount('#app')
