﻿namespace TemplateCore.Service
{
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
  using Model;
  using Repository;
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  /// Class UserService.
  /// </summary>
  public class UserService : IUserService
  {
    /// <summary>
    /// UnitOfWork
    /// </summary>
    private IUnitOfWork<TemplateDbContext> unitOfWork = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="dataContext">The data context.</param>
    /// <exception cref="ArgumentNullException">dataContext</exception>
    public UserService(IUnitOfWork<TemplateDbContext> unitOfWork)
    {
      if (unitOfWork == null)
      {
        throw new ArgumentNullException("unitOfWork");
      }

      this.unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Determines whether this instance [can insert email] the specified email.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns><c>true</c> if this instance [can insert email] the specified email; otherwise, <c>false</c>.</returns>
    public bool CanInsertEmail(string email)
    {
      bool canInsert = false;

      var query = this.unitOfWork.UserRepository.FindBy(x => x.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));

      //var query = this.dataContext.Users.Where(x => x.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();

      if (query == null)
      {
        // no data found.
        canInsert = true;
      }

      return canInsert;
    }

    /// <summary>
    /// Determines whether this instance [can insert user name] the specified user name.
    /// </summary>
    /// <param name="userName">Name of the user.</param>
    /// <returns><c>true</c> if this instance [can insert user name] the specified user name; otherwise, <c>false</c>.</returns>
    public bool CanInsertUserName(string userName)
    {
      bool canInsert = false;

      var query = this.unitOfWork.UserRepository.FindBy(x => x.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));

      //var query = this.dataContext.Users.Where(x => x.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();

      if (query == null)
      {
        // no data found.
        canInsert = true;
      }

      return canInsert;
    }

    /// <summary>
    /// Deletes the user by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public void DeleteById(string id)
    {
      var query = this.unitOfWork.UserRepository.FindBy(x => x.Id.Equals(id, StringComparison.CurrentCultureIgnoreCase));
      //var user = this.dataContext.Users.Where(x => x.Id.Equals(id, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

      //this.dataContext.Users.Remove(user);
      this.unitOfWork.UserRepository.Delete(query);

      //this.dataContext.SaveChanges();
      this.unitOfWork.Commit();
    }

    /// <summary>
    /// Exists the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    public bool Exist(string id)
    {
      bool exists = false;

      var query = this.unitOfWork.UserRepository.FindBy(x => x.Id.Equals(id, StringComparison.CurrentCultureIgnoreCase));
      //var query = this.dataContext.Users.Where(x => x.Id == id).FirstOrDefault();

      if (query != null)
      {
        exists = true;
      }

      return exists;
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>List of type <see cref="AspNetUser" />.</returns>
    public IEnumerable<AspNetUser> GetAll()
    {
      // if users are like a normal context...
      var users = this.unitOfWork.UserRepository.GetAll().ToList(); // this.dataContext.Users.ToList();

      List<AspNetUser> aspNetUsers = new List<AspNetUser>();

      foreach (var user in users)
      {
        // roles are null... dont know why, will do it the hard way.
        var aspNetUser = new AspNetUser { Id = new Guid(user.Id), Name = user.Name, UserName = user.UserName, Roles = GetRolesString(user.Id) };

        aspNetUsers.Add(aspNetUser);
      }

      return aspNetUsers;
      
      //return users.Select(x => new AspNetUser { Id = new Guid(x.Id), UserName = x.UserName, Name = x.Name, Roles = GetRolesString(x.Roles.ToList()) }).ToList();
    }

    /// <summary>
    /// Gets the roles string.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>System.String.</returns>
    private string GetRolesString(string userId)
    {
      List<string> roleNames = new List<string>();

      //foreach (var role in roles)
      //{
      //  var query = this.unitOfWork.RoleRepository.FindBy(x => x.Id.Equals(role.RoleId));
      //  roleNames.Add(query.Name);
      //}

      // query all the UserRoles.
      var userRoles = this.unitOfWork.UseRolesRepository.GetAll().ToList() ;

      foreach (var userRole in userRoles)
      {
        if (userRole.UserId.Equals(userId))
        {
          // if the UserRoles is equal...
          var role = this.unitOfWork.RoleRepository.FindBy(x => x.Id.Equals(userRole.RoleId));

          // finally...
          roleNames.Add(role.Name);
        }
      }

      return string.Join(",", roleNames);
    }

    /// <summary>
    /// Gets all roles.
    /// </summary>
    /// <returns>List of type <see cref="IdentityRole"/>.</returns>
    public IEnumerable<IdentityRole> GetAllRoles()
    {
      return this.unitOfWork.RoleRepository.GetAll().ToList();
    }

    /// <summary>
    /// Inserts a new user.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <param name="userName">Name of the user.</param>
    /// <param name="name">The name.</param>
    /// <param name="roleIds">The role ids.</param>
    public void Insert(string email, string userName, string name, IEnumerable<string> roleIds)
    {
      // create new User
      ApplicationUser newUser = new ApplicationUser
      {
        // set data.
        Name = name,
        UserName = userName,
        NormalizedUserName = userName,
        Email = email,
        NormalizedEmail = email,
        EmailConfirmed = true,
        LockoutEnabled = false,
        SecurityStamp = Guid.NewGuid().ToString()
      };

      // password by default.
      var password = new PasswordHasher<ApplicationUser>();
      var hashed = password.HashPassword(newUser, "1122334455");
      newUser.PasswordHash = hashed;

      // attach to context.
      // this.dataContext.Users.Add(newUser);
      this.unitOfWork.UserRepository.Insert(newUser);

      // save changes in context.
      //this.unitOfWork.Commit();

      // query for the user by its username, or check if the newUser's Id is updated after commit.
      //var insertedUser = this.unitOfWork.UserRepository.FindBy(x => x.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));

      var allUserRoles = this.unitOfWork.UseRolesRepository.GetAll().ToList();

      // query for the selected roles by the id and then add to the repository.
      foreach(var roleId in roleIds)
      {
        var role = this.unitOfWork.RoleRepository.FindBy(x => x.Id.Equals(roleId, StringComparison.CurrentCultureIgnoreCase));

        if (role != null)
        {
          // add to user roles.
          this.unitOfWork.UseRolesRepository.Insert(new IdentityUserRole<string> { RoleId = role.Id, UserId = newUser.Id });
        }
      }

      // save changes made to AspNetUsers table and AspNetUserRoles
      this.unitOfWork.Commit();
    }
  }
}
