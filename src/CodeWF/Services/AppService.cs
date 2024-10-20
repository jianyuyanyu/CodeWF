﻿using CodeWF.Options;
using CodeWF.Tools.Extensions;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using CodeWF.Models;

namespace CodeWF.Services;

public class AppService
{
    private readonly SiteOption _siteInfo;
    private List<DocItem>? _docItems;
    private List<CategotyItem>? _categotyItems;
    private List<BlogPost>? _blogPosts;

    public AppService(IOptions<SiteOption> siteOption)
    {
        _siteInfo = siteOption.Value;
    }

    public async Task<List<DocItem>?> GetAllDocItemsAsync()
    {
        if (_docItems?.Any() == true)
        {
            return _docItems;
        }

        var filePath = Path.Combine(_siteInfo.LocalAssetsDir, "site", "doc", "doc.json");
        if (!File.Exists(filePath))
        {
            return _docItems;
        }

        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContent.FromJson(out _docItems, out var msg);
        return _docItems;
    }

    public async Task<DocItem?> GetDocItemAsync(string slug)
    {
        async Task ReadContentAsync(DocItem item, string? parentDir = default)
        {
            if (!string.IsNullOrWhiteSpace(item.Content))
            {
                return;
            }

            var contentPath = string.Empty;
            contentPath = string.IsNullOrWhiteSpace(parentDir)
                ? Path.Combine(_siteInfo.LocalAssetsDir, "site", "doc", $"{item.Slug}.md")
                : Path.Combine(_siteInfo.LocalAssetsDir, "site", "doc", parentDir, $"{item.Slug}.md");

            if (File.Exists(contentPath))
            {
                item.Content = await File.ReadAllTextAsync(contentPath);
            }
        }

        var items = await GetAllDocItemsAsync();
        foreach (var item in items)
        {
            if (item.Slug == slug)
            {
                await ReadContentAsync(item);
                return item;
            }

            if (item.Children?.Any() != true) continue;

            foreach (var itemChild in item.Children.Where(itemChild => itemChild.Slug == slug))
            {
                await ReadContentAsync(itemChild, item.Slug);
                return itemChild;
            }
        }

        var first = items.FirstOrDefault();
        await ReadContentAsync(first);

        return first;
    }

    public async Task<List<CategotyItem>?> GetAllCategoryItemsAsync()
    {
        if (_categotyItems?.Any() == true)
        {
            return _categotyItems;
        }

        var filePath = Path.Combine(_siteInfo.LocalAssetsDir, "site", "category.json");
        if (!File.Exists(filePath))
        {
            return _categotyItems;
        }

        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContent.FromJson(out _categotyItems, out var msg);
        return _categotyItems;
    }


    public async Task<List<BlogPost>?> GetAllBlogPostsAsync()
    {
        if (_blogPosts?.Any() == true)
        {
            return _blogPosts;
        }

        _blogPosts = new();
        var endYear = DateTime.Now.Year;

        for (var start = _siteInfo.StartYear; start <= endYear; start++)
        {
            var postDir = Path.Combine(_siteInfo.LocalAssetsDir, start.ToString());
            var postFiles = Directory.GetFiles(postDir, "*.md", SearchOption.AllDirectories);
            foreach (var postFile in postFiles)
            {
                var blogPost = await ReadBlogPostAsync(postFile);
                _blogPosts.Add(blogPost);
            }
        }

        return _blogPosts;
    }

    public async Task<PageData<BlogPost>?> GetPostByCategory(int pageIndex, int pageSize, string slug)
    {
        var cat = (await GetAllCategoryItemsAsync())?.FirstOrDefault(cat => cat.Slug == slug);
        if (cat == null)
        {
            return default;
        }

        var posts = (await GetAllBlogPostsAsync())
            ?.Where(post => post.Categories?.Contains(cat.Name) == true);
        var total = posts.Count();

        var postDatas = posts
            .OrderByDescending(post => post.Lastmod)
            .ThenByDescending(post => post.Date)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();
        return new PageData<BlogPost>(pageIndex, pageSize, total, postDatas);
    }

    public async Task<List<BlogPost>?> GetBannerPostAsync()
    {
        var posts = (await GetAllBlogPostsAsync())
                                                    ?.Where(post => post.Banner)
                                                    .OrderByDescending(post=>post.Date)
                                                    .ToList();
        return posts;
    }

    public async Task<BlogPost?> GetPostBySlug(string slug)
    {
        var post = (await GetAllBlogPostsAsync())?.FirstOrDefault(cat => cat.Slug == slug);
        return post;
    }

    public static async Task<BlogPost> ReadBlogPostAsync(string markdownFilePath)
    {
        var markdown = await File.ReadAllTextAsync(markdownFilePath);
        var endOfFrontMatter = markdown.IndexOf("---", 3, StringComparison.Ordinal);
        if (endOfFrontMatter == -1)
        {
            throw new InvalidOperationException("Invalid markdown format. No ending '---' found for Front Matter.");
        }

        var frontMatterText = markdown[..(endOfFrontMatter + 3)];
        var markdownContent = markdown[(endOfFrontMatter + 3)..].Trim();
        if (frontMatterText.StartsWith("---"))
        {
            frontMatterText = frontMatterText[3..].Trim();
        }

        if (frontMatterText.EndsWith("---"))
        {
            frontMatterText = frontMatterText[..^3].Trim();
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        BlogPost blogPost;
        try
        {
            blogPost = deserializer.Deserialize<BlogPost>(frontMatterText);
        }
        catch (Exception ex)
        {
            string a = ex.Message;

            blogPost = new BlogPost();
        }

        blogPost.Content = markdownContent;

        return blogPost;
    }
}