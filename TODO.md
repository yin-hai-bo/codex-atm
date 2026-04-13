# TODO List

- 支持多语言。
- 归档线程删除与 `state_5.sqlite` 中 `threads` 元数据的一致性问题尚未决策：
  删除归档文件但不更新 `state_5.sqlite` 会留下孤儿记录；删除文件时同步删除 `threads` 记录，又会导致用户从回收站恢复 `.jsonl` 文件后无法恢复对应标题等 metadata。需要明确最终策略。
