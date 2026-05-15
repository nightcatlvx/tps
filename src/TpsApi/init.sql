-- ============================================================
-- Tps 数据库初始化脚本
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- 1. 服务配置表（定义第三方服务）
-- ============================================================
DROP TABLE IF EXISTS `tps_service_config`;
CREATE TABLE `tps_service_config` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `service_code` varchar(50) NOT NULL COMMENT '服务标识',
  `service_type` tinyint NOT NULL DEFAULT 1 COMMENT '1=HTTP, 2=SDK',
  `config_json` varchar(2000) NOT NULL COMMENT '服务配置 JSON',
  `base_url` varchar(255) NOT NULL DEFAULT '' COMMENT '基础服务地址',
  `enabled` tinyint NOT NULL COMMENT '1=启用, 0=禁用',
  `is_deleted` tinyint NOT NULL COMMENT '0=正常, 1=删除',
  `version_no` int NOT NULL DEFAULT 1,
  `create_time` datetime NOT NULL,
  `created_by` varchar(50) NOT NULL,
  `update_time` datetime NOT NULL,
  `updated_by` varchar(50) NOT NULL,
  `remark` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniq_service_code`(`service_code` ASC) USING BTREE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_general_ci;

-- ------------------------------------------------------------
-- 示例数据（请将密钥替换为你自己的账号）
-- ------------------------------------------------------------
INSERT INTO `tps_service_config` VALUES
(1, 'youdao', 1,
 '{"AppKey":"你的有道AppKey","AppSecret":"你的有道AppSecret"}',
 'https://openapi.youdao.com',
 1, 0, 1, NOW(), 'system', NOW(), 'system', '有道翻译'),
(2, 'alibaba_ocr', 2,
 '{"AccessKeyId":"你的阿里云AccessKey","AccessKeySecret":"你的阿里云AccessSecret","Endpoint":"ocr-api.cn-hangzhou.aliyuncs.com"}',
 '',
 1, 0, 1, NOW(), 'system', NOW(), 'system', '阿里云OCR'),
(3, 'qywx', 1,
 '{"CorpId":"你的企业ID","Secret":"你的应用Secret","AgentId":"你的应用AgentId"}',
 'https://qyapi.weixin.qq.com',
 1, 0, 1, NOW(), 'system', NOW(), 'system', '企业微信');

-- ============================================================
-- 2. 功能配置表（定义每个服务下的 API 路由）
-- ============================================================
DROP TABLE IF EXISTS `tps_func_config`;
CREATE TABLE `tps_func_config` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `func_code` varchar(50) NOT NULL COMMENT '功能标识',
  `service_id` int UNSIGNED NOT NULL COMMENT '关联 service_config.id',
  `path` varchar(255) NOT NULL COMMENT '路由地址',
  `enabled` tinyint NOT NULL COMMENT '1=启用, 0=禁用',
  `is_deleted` tinyint NOT NULL COMMENT '0=正常, 1=删除',
  `version_no` int NOT NULL DEFAULT 1,
  `create_time` datetime NOT NULL,
  `created_by` varchar(50) NOT NULL,
  `update_time` datetime NOT NULL,
  `updated_by` varchar(50) NOT NULL,
  `remark` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniq_func_code`(`func_code` ASC) USING BTREE,
  INDEX `idx_service_id`(`service_id` ASC) USING BTREE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_general_ci;

-- ------------------------------------------------------------
-- 示例数据
-- func_code 与 IExternalClient 接口方法上的 [FuncCode] 一一对应
-- service_id 指向上面 tps_service_config 的 id
-- ------------------------------------------------------------
INSERT INTO `tps_func_config` VALUES
(1, 'youdao_translate',      1, '/api',                      1, 0, 1, NOW(), 'system', NOW(), 'system', '有道翻译'),
(2, 'alibaba_ocr_keywords',  2, '',                          1, 0, 1, NOW(), 'system', NOW(), 'system', '阿里OCR关键词'),
(3, 'qywx_get_userid',       3, '/cgi-bin/user/getuserid',   1, 0, 1, NOW(), 'system', NOW(), 'system', '企微-手机号换userid'),
(4, 'qywx_send_message',     3, '/cgi-bin/message/send',     1, 0, 1, NOW(), 'system', NOW(), 'system', '企微-发送消息'),
(5, 'qywx_get_token',        3, '/cgi-bin/gettoken',         1, 0, 1, NOW(), 'system', NOW(), 'system', '企微-获取access_token');

-- ============================================================
-- 3. 限流规则表（按 func_id 配置令牌桶参数）
-- ============================================================
DROP TABLE IF EXISTS `tps_func_rate_limit_rule`;
CREATE TABLE `tps_func_rate_limit_rule` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `func_id` int UNSIGNED NOT NULL COMMENT '关联 func_config.id',
  `window_seconds` int NOT NULL COMMENT '时间窗口(秒)',
  `max_requests` int NOT NULL COMMENT '窗口内最大请求数',
  `burst_per_second` int NOT NULL COMMENT '每秒补充令牌数(防突刺)',
  `enabled` tinyint NOT NULL COMMENT '1=启用, 0=禁用',
  `is_deleted` tinyint NOT NULL COMMENT '0=正常, 1=删除',
  `version_no` int NOT NULL DEFAULT 1,
  `create_time` datetime NOT NULL,
  `created_by` varchar(50) NOT NULL,
  `update_time` datetime NOT NULL,
  `updated_by` varchar(50) NOT NULL,
  `remark` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniq_func_id`(`func_id` ASC) USING BTREE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_general_ci;

-- ------------------------------------------------------------
-- 示例数据
-- func_id 对应上面 tps_func_config 的 id
-- 令牌产生速率 = max_requests / window_seconds（即每秒填充多少个令牌）
-- ------------------------------------------------------------
INSERT INTO `tps_func_rate_limit_rule` VALUES
(1, 1, 10, 1, 1, 1, 0, 1, NOW(), 'system', NOW(), 'system', '有道翻译: 10秒内最多1次请求');

-- ============================================================
-- 4. 企业微信用户缓存表（手机号 → userid）
-- ============================================================
DROP TABLE IF EXISTS `tps_wechat_user_data`;
CREATE TABLE `tps_wechat_user_data` (
  `id` int UNSIGNED NOT NULL AUTO_INCREMENT,
  `mobile` varchar(20) NOT NULL COMMENT '手机号',
  `wxid` varchar(128) NOT NULL COMMENT '企微 userid',
  `create_time` datetime NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniq_mobile`(`mobile` ASC) USING BTREE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_general_ci;

SET FOREIGN_KEY_CHECKS = 1;
