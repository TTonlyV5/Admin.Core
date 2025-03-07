﻿using Microsoft.AspNetCore.Mvc;
using ZhonTai.Admin.Core.Attributes;
using ZhonTai.Admin.Core.Dto;
using ZhonTai.Admin.Domain.Api;
using ZhonTai.Admin.Services.Api.Dto;
using ZhonTai.DynamicApi;
using ZhonTai.DynamicApi.Attributes;
using ZhonTai.Admin.Core.Consts;
using ZhonTai.Admin.Repositories;
using ZhonTai.Admin.Core.Configs;
using ZhonTai.Common.Extensions;
using ZhonTai.Admin.Resources;
using ZhonTai.Admin.Core.Db;
using Microsoft.Extensions.Options;

namespace ZhonTai.Admin.Services.Api;

/// <summary>
/// 接口服务
/// </summary>
[Order(90)]
[DynamicApi(Area = AdminConsts.AreaName)]
public class ApiService : BaseService, IApiService, IDynamicApi
{
    private readonly AdminRepositoryBase<ApiEntity> _apiRep;
    private readonly AdminLocalizer _adminLocalizer;
    private readonly Lazy<IOptions<AppConfig>> _appConfig;

    public ApiService(
        AdminRepositoryBase<ApiEntity> apiRep, 
        AdminLocalizer adminLocalizer,
        Lazy<IOptions<AppConfig>> appConfig
    )
    {
        _apiRep = apiRep;
        _adminLocalizer = adminLocalizer;
        _appConfig = appConfig;
    }

    private async Task ClearCacheAsync()
    {
        await Cache.DelAsync(CacheKeys.ApiList);
    }

    /// <summary>
    /// 查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<ApiGetOutput> GetAsync(long id)
    {
        var result = await _apiRep.GetAsync<ApiGetOutput>(id);
        return result;
    }

    /// <summary>
    /// 查询列表
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<List<ApiGetListOutput>> GetListAsync(string key)
    {
        var data = await _apiRep
            .WhereIf(key.NotNull(), a => a.Path.Contains(key) || a.Label.Contains(key))
            .OrderBy(a => a.ParentId)
            .OrderBy(a => a.Sort)
            .ToListAsync<ApiGetListOutput>();

        return data;
    }

    /// <summary>
    /// 查询分页
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<PageOutput<ApiEntity>> GetPageAsync(PageInput<ApiGetPageInput> input)
    {
        var key = input.Filter?.Label;

        var list = await _apiRep.Select
        .WhereDynamicFilter(input.DynamicFilter)
        .WhereIf(key.NotNull(), a => a.Path.Contains(key) || a.Label.Contains(key))
        .Count(out var total)
        .OrderBy(a => a.ParentId)
        .OrderBy(a => a.Sort)
        .Page(input.CurrentPage, input.PageSize)
        .ToListAsync();

        var data = new PageOutput<ApiEntity>()
        {
            List = list,
            Total = total
        };

        return data;
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<long> AddAsync(ApiAddInput input)
    {
        var path = input.Path;

        var entity = await _apiRep.Select.DisableGlobalFilter(FilterNames.Delete)
            .Where(w => w.Path.Equals(path) && w.IsDeleted).FirstAsync();

        if (entity?.Id > 0)
        {
            Mapper.Map(input, entity);
            entity.IsDeleted = false;
            entity.Enabled = true;
            await _apiRep.UpdateDiy.DisableGlobalFilter(FilterNames.Delete).SetSource(entity).ExecuteAffrowsAsync();

            return entity.Id;
        }
        entity = Mapper.Map<ApiEntity>(input);

        if (entity.Sort == 0)
        {
            var sort = await _apiRep.Select.DisableGlobalFilter(FilterNames.Delete).Where(a => a.ParentId == input.ParentId).MaxAsync(a => a.Sort);
            entity.Sort = sort + 1;
        }

        await _apiRep.InsertAsync(entity);

        await ClearCacheAsync();

        return entity.Id;
    }

    /// <summary>
    /// 修改
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task UpdateAsync(ApiUpdateInput input)
    {
        var entity = await _apiRep.GetAsync(input.Id);
        if (!(entity?.Id > 0))
        {
            throw ResultOutput.Exception(_adminLocalizer["接口不存在"]);
        }

        Mapper.Map(input, entity);
        await _apiRep.UpdateAsync(entity);

        await ClearCacheAsync();
    }
    /// <summary>
    /// 设置启用接口日志
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task SetEnableLogAsync(ApiSetEnableLogInput input)
    {
        await _apiRep.UpdateDiy.Set(a => new ApiEntity
        {
            EnabledLog = input.EnabledLog,
            ModifiedUserId = User.Id,
            ModifiedUserName = User.UserName,
            ModifiedUserRealName = User.Name,
            ModifiedTime = DbHelper.ServerTime
        })
        .WhereDynamic(input.ApiId)
        .ExecuteAffrowsAsync();

        await ClearCacheAsync();
    }
    /// <summary>
    /// 设置启用请求参数
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task SetEnableParamsAsync(ApiSetEnableParamsInput input)
    {
        await _apiRep.UpdateDiy.Set(a => new ApiEntity
        {
            EnabledParams = input.EnabledParams,
            ModifiedUserId = User.Id,
            ModifiedUserName = User.UserName,
            ModifiedUserRealName = User.Name,
            ModifiedTime = DbHelper.ServerTime
        })
        .WhereDynamic(input.ApiId)
        .ExecuteAffrowsAsync();

        await ClearCacheAsync();
    }

    /// <summary>
    /// 设置启用响应结果
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task SetEnableResultAsync(ApiSetEnableResultInput input)
    {
        await _apiRep.UpdateDiy.Set(a => new ApiEntity
        {
            EnabledResult = input.EnabledResult,
            ModifiedUserId = User.Id,
            ModifiedUserName = User.UserName,
            ModifiedUserRealName = User.Name,
            ModifiedTime = DbHelper.ServerTime
        })
        .WhereDynamic(input.ApiId)
        .ExecuteAffrowsAsync();

        await ClearCacheAsync();
    }

    /// <summary>
    /// 彻底删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(long id)
    {
        await _apiRep.DeleteAsync(a => a.Id == id);

        await ClearCacheAsync();
    }

    /// <summary>
    /// 批量彻底删除
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task BatchDeleteAsync(long[] ids)
    {
        await _apiRep.DeleteAsync(a => ids.Contains(a.Id));

        await ClearCacheAsync();
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete]
    public async Task SoftDeleteAsync(long id)
    {
        await _apiRep.SoftDeleteAsync(id);

        await ClearCacheAsync();
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task BatchSoftDeleteAsync(long[] ids)
    {
        await _apiRep.SoftDeleteAsync(ids);

        await ClearCacheAsync();
    }

    /// <summary>
    /// 同步
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [AdminTransaction]
    public virtual async Task SyncAsync(ApiSyncInput input)
    {
        if (!(input?.Apis?.Count > 0)) return;

        //查询分组下所有模块的api
        var groupPaths = input.Apis.FindAll(a => a.ParentPath.IsNull()).Select(a => a.Path);
        var groups = await _apiRep.Select.DisableGlobalFilter(FilterNames.Delete)
            .Where(a => a.ParentId == 0 && groupPaths.Contains(a.Path)).ToListAsync();
        var groupIds = groups.Select(a => a.Id);
        var modules = await _apiRep.Select.DisableGlobalFilter(FilterNames.Delete)
            .Where(a => groupIds.Contains(a.ParentId)).ToListAsync();
        var moduleIds = modules.Select(a => a.Id);
        var apis = await _apiRep.Select.DisableGlobalFilter(FilterNames.Delete)
            .Where(a=> moduleIds.Contains(a.ParentId)).ToListAsync();

        apis = groups.Concat(modules).Concat(apis).ToList();
        var paths = apis.Select(a => a.Path).ToList();

        //path处理
        foreach (var api in input.Apis)
        {
            api.Path = api.Path?.Trim().ToLower();
            api.ParentPath = api.ParentPath?.Trim().ToLower();
        }

        #region 执行插入

        //执行父级api插入
        var parentApis = input.Apis.FindAll(a => a.ParentPath.IsNull());
        var pApis = (from a in parentApis where !paths.Contains(a.Path) select a).ToList();
        if (pApis.Count > 0)
        {
            var insertPApis = Mapper.Map<List<ApiEntity>>(pApis);
            insertPApis = await _apiRep.InsertAsync(insertPApis);
            apis.AddRange(insertPApis);
        }

        //执行子级api插入
        var childApis = input.Apis.FindAll(a => a.ParentPath.NotNull());
        var cApis = (from a in childApis where !paths.Contains(a.Path) select a).ToList();
        if (cApis.Count > 0)
        {
            var insertCApis = Mapper.Map<List<ApiEntity>>(cApis);
            insertCApis = await _apiRep.InsertAsync(insertCApis);
            apis.AddRange(insertCApis);
        }

        #endregion 执行插入

        #region 修改和禁用

        {
            //父级api修改
            ApiEntity a;
            List<string> labels;
            string label;
            string desc;
            for (int i = 0, len = parentApis.Count; i < len; i++)
            {
                ApiSyncModel api = parentApis[i];
                a = apis.Find(a => a.Path == api.Path);
                if (a?.Id > 0)
                {
                    labels = api.Label?.Split("\r\n")?.ToList();
                    label = labels != null && labels.Count > 0 ? labels[0] : string.Empty;
                    desc = labels != null && labels.Count > 1 ? string.Join("\r\n", labels.GetRange(1, labels.Count - 1)) : string.Empty;
                    a.ParentId = 0;
                    a.Label = label;
                    a.Description = desc;
                    a.Sort = i + 1;
                    a.Enabled = true;
                    a.IsDeleted = false;
                }
            }
        }

        {
            //子级api修改
            ApiEntity a;
            ApiEntity pa;
            List<string> labels;
            string label;
            string desc;
            for (int i = 0, len = childApis.Count; i < len; i++)
            {
                ApiSyncModel api = childApis[i];
                a = apis.Find(a => a.Path == api.Path);
                pa = apis.Find(a => a.Path == api.ParentPath);
                if (a?.Id > 0)
                {
                    labels = api.Label?.Split("\r\n")?.ToList();
                    label = labels != null && labels.Count > 0 ? labels[0] : string.Empty;
                    desc = labels != null && labels.Count > 1 ? string.Join("\r\n", labels.GetRange(1, labels.Count - 1)) : string.Empty;

                    a.ParentId = pa.Id;
                    a.Label = label;
                    a.Description = desc;
                    a.HttpMethods = api.HttpMethods;
                    a.Sort = i + 1;
                    a.Enabled = true;
                    a.IsDeleted = false;
                }
            }
        }

        {
            //模块和api禁用
            var inputPaths = input.Apis.Select(a => a.Path).ToList();
            var disabledApis = (from a in apis where !inputPaths.Contains(a.Path) select a).ToList();
            if (disabledApis.Count > 0)
            {
                foreach (var api in disabledApis)
                {
                    api.Enabled = false;
                }
            }
        }

        #endregion 修改和禁用

        //批量更新
        await _apiRep.UpdateDiy.DisableGlobalFilter(FilterNames.Delete).SetSource(apis)
        .UpdateColumns(a => new { a.ParentId, a.Label, a.HttpMethods, a.Description, a.Sort, a.Enabled, a.IsDeleted, a.ModifiedTime })
        .ExecuteAffrowsAsync();

        await ClearCacheAsync();
    }

    /// <summary>
    /// 获得项目列表
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [NoOperationLog]
    public List<ProjectConfig> GetProjects(string suffix = "/swagger")
    {
        var routePrefix = _appConfig.Value.Value.ApiUI?.RoutePrefix;
        if (routePrefix.IsNull())
        {
            if (routePrefix.EndsWith(suffix))
            {
                int suffixIndex = routePrefix.LastIndexOf(suffix);
                routePrefix = routePrefix.Substring(0, suffixIndex);
            }
        }

        return new List<ProjectConfig>
        {
            new ProjectConfig
            {
                Code = routePrefix
            }
        };
    }
}