using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.ObjectModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Manages role-permission assignments using a module/action matrix.
/// </summary>
public sealed partial class RBACViewModel : ObservableObject
{
    private readonly IRBACService  _rbacService;
    private readonly IAuditService _auditService;

    // ── Collections ──────────────────────────────────────────────────────────
    public ObservableCollection<AppRole>          Roles             { get; } = new();
    public ObservableCollection<AppPermission>    AllPermissions    { get; } = new();
    public ObservableCollection<ModulePermissions> PermissionMatrix { get; } = new();

    // ── Selection ─────────────────────────────────────────────────────────────
    [ObservableProperty] private AppRole? _selectedRole;

    // ── Form for new role ─────────────────────────────────────────────────────
    [ObservableProperty] private string _newRoleName        = string.Empty;
    [ObservableProperty] private string _newRoleDescription = string.Empty;

    // ── Status ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private bool   _hasError;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public RBACViewModel(IRBACService rbacService, IAuditService auditService)
    {
        _rbacService  = rbacService;
        _auditService = auditService;
    }

    partial void OnSelectedRoleChanged(AppRole? value) => BuildMatrix(value);

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var roles = await _rbacService.GetAllRolesAsync();
            var perms = await _rbacService.GetAllPermissionsAsync();

            Roles.Clear();
            foreach (var r in roles) Roles.Add(r);

            AllPermissions.Clear();
            foreach (var p in perms) AllPermissions.Add(p);

            SelectedRole = Roles.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SavePermissionsAsync()
    {
        if (SelectedRole is null) return;

        IsSaving = true;
        HasError = false;
        try
        {
            var selectedIds = GetSelectedPermissionIds();
            await _rbacService.UpdateRolePermissionsAsync(SelectedRole.Id, selectedIds);
            await _auditService.LogActionAsync(0, "RolePermissionsUpdated", "Security", "Role",
                SelectedRole.Id.ToString(), newValue: SelectedRole.Name);
            StatusMessage = $"Permissions saved for role '{SelectedRole.Name}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName))
        {
            StatusMessage = "Role name is required.";
            HasError = true;
            return;
        }

        IsSaving = true;
        HasError = false;
        try
        {
            var role = await _rbacService.CreateRoleAsync(NewRoleName, NewRoleDescription, Array.Empty<int>());
            Roles.Add(role);
            SelectedRole      = role;
            NewRoleName       = string.Empty;
            NewRoleDescription = string.Empty;
            StatusMessage     = $"Role '{role.Name}' created successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ── Matrix Builder ────────────────────────────────────────────────────────

    private void BuildMatrix(AppRole? role)
    {
        PermissionMatrix.Clear();
        if (role is null) return;

        foreach (PermissionModule mod in Enum.GetValues<PermissionModule>())
        {
            var row = new ModulePermissions { Module = mod };

            foreach (var perm in AllPermissions.Where(p => p.Module == mod))
            {
                bool granted = role.PermissionIds.Contains(perm.Id);
                switch (perm.Action)
                {
                    case PermissionAction.View:   row.CanView   = granted; break;
                    case PermissionAction.Create: row.CanCreate = granted; break;
                    case PermissionAction.Edit:   row.CanEdit   = granted; break;
                    case PermissionAction.Delete: row.CanDelete = granted; break;
                    case PermissionAction.Print:  row.CanPrint  = granted; break;
                    case PermissionAction.Export: row.CanExport = granted; break;
                }
            }

            PermissionMatrix.Add(row);
        }
    }

    private IEnumerable<int> GetSelectedPermissionIds()
    {
        var ids = new List<int>();
        foreach (var row in PermissionMatrix)
        {
            foreach (var perm in AllPermissions.Where(p => p.Module == row.Module))
            {
                bool granted = perm.Action switch
                {
                    PermissionAction.View   => row.CanView,
                    PermissionAction.Create => row.CanCreate,
                    PermissionAction.Edit   => row.CanEdit,
                    PermissionAction.Delete => row.CanDelete,
                    PermissionAction.Print  => row.CanPrint,
                    PermissionAction.Export => row.CanExport,
                    _                       => false
                };
                if (granted) ids.Add(perm.Id);
            }
        }
        return ids;
    }
}
