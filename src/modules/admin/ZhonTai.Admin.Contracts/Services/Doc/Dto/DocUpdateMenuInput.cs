﻿using System.ComponentModel.DataAnnotations;
using ZhonTai.Admin.Core.Validators;

namespace ZhonTai.Admin.Services.Doc.Dto;

/// <summary>
/// 更新菜单
/// </summary>
public class DocUpdateMenuInput : DocAddMenuInput
{
    /// <summary>
    /// 编号
    /// </summary>
    [Required]
    [ValidateRequired("请选择菜单")]
    public long Id { get; set; }
}