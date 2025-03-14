﻿using ZhonTai.Admin.Domain.Doc;

namespace ZhonTai.Admin.Services.Doc.Dto;

/// <summary>
/// 文档列表
/// </summary>
public class DocListOutput
{
    /// <summary>
    /// 编号
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 父级节点
    /// </summary>
    public long ParentId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public DocType Type { get; set; }

    /// <summary>
    /// 命名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 组打开
    /// </summary>
    public bool? Opened { get; set; }
}