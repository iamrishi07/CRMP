using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using CRMP.Helpers;
using CRMP.Models;

namespace CRMP.DAL
{
    public class UserRepository
    {
        public User GetById(int userId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT u.USER_ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.PHONE,
                       u.DIVISION_ID, d.DIVISION_NAME, u.DESIGNATION,
                       u.BUILDING_ID, b.BUILDING_NAME, u.FLOOR_NUMBER,
                       u.IS_ACTIVE, u.LAST_LOGIN_AT, u.CREATED_AT
                FROM   USERS u
                JOIN   DIVISIONS d ON d.DIVISION_ID = u.DIVISION_ID
                LEFT JOIN BUILDINGS b ON b.BUILDING_ID = u.BUILDING_ID
                WHERE  u.USER_ID = :P_USER_ID",
                new[] { OracleHelper.ParamInt("P_USER_ID", userId) });

            return dt.Rows.Count > 0 ? MapUser(dt.Rows[0]) : null;
        }

        public User GetByUsername(string username)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT u.USER_ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.PHONE,
                       u.DIVISION_ID, d.DIVISION_NAME, u.DESIGNATION,
                       u.BUILDING_ID, b.BUILDING_NAME, u.FLOOR_NUMBER,
                       u.IS_ACTIVE, u.LAST_LOGIN_AT, u.CREATED_AT
                FROM   USERS u
                JOIN   DIVISIONS d ON d.DIVISION_ID = u.DIVISION_ID
                LEFT JOIN BUILDINGS b ON b.BUILDING_ID = u.BUILDING_ID
                WHERE  LOWER(u.USERNAME) = LOWER(:P_USERNAME) AND u.IS_ACTIVE = 1",
                new[] { OracleHelper.ParamStr("P_USERNAME", username) });

            return dt.Rows.Count > 0 ? MapUser(dt.Rows[0]) : null;
        }

        public string GetPasswordHash(int userId)
        {
            var result = OracleHelper.ExecuteScalarSql(
                "SELECT PASSWORD_HASH FROM USERS WHERE USER_ID = :P_USER_ID",
                new[] { OracleHelper.ParamInt("P_USER_ID", userId) });
            return OracleHelper.ToString(result);
        }

        public List<UserRole> GetRoles(int userId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT ur.USER_ROLE_ID, ur.USER_ID, ur.ROLE_ID,
                       r.ROLE_CODE, r.ROLE_NAME, r.SORT_ORDER,
                       ur.DIVISION_ID, d.DIVISION_NAME, ur.IS_ACTIVE
                FROM   USER_ROLES ur
                JOIN   ROLES r ON r.ROLE_ID = ur.ROLE_ID
                LEFT JOIN DIVISIONS d ON d.DIVISION_ID = ur.DIVISION_ID
                WHERE  ur.USER_ID = :P_USER_ID AND ur.IS_ACTIVE = 1
                ORDER BY r.SORT_ORDER",
                new[] { OracleHelper.ParamInt("P_USER_ID", userId) });

            var roles = new List<UserRole>();
            foreach (DataRow row in dt.Rows)
                roles.Add(MapUserRole(row));

            // Also include active delegations coming IN to this user
            var delegated = GetDelegatedRoles(userId);
            roles.AddRange(delegated);
            return roles;
        }

        private List<UserRole> GetDelegatedRoles(int userId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT rd.DELEGATION_ID, rd.DELEGATEE_USER_ID AS USER_ID,
                       rd.ROLE_ID, r.ROLE_CODE, r.ROLE_NAME, r.SORT_ORDER,
                       rd.DIVISION_ID, d.DIVISION_NAME, 1 AS IS_ACTIVE
                FROM   ROLE_DELEGATIONS rd
                JOIN   ROLES r ON r.ROLE_ID = rd.ROLE_ID
                LEFT JOIN DIVISIONS d ON d.DIVISION_ID = rd.DIVISION_ID
                WHERE  rd.DELEGATEE_USER_ID = :P_USER_ID
                  AND  rd.IS_ACTIVE = 1
                  AND  TRUNC(SYSDATE) BETWEEN rd.START_DATE AND rd.END_DATE",
                new[] { OracleHelper.ParamInt("P_USER_ID", userId) });

            var roles = new List<UserRole>();
            foreach (DataRow row in dt.Rows)
            {
                var ur = MapUserRole(row);
                ur.RoleName = $"{ur.RoleName} (Delegated)";
                roles.Add(ur);
            }
            return roles;
        }

        public List<User> SearchInDivision(string query, int divisionId, int excludeUserId = 0)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT u.USER_ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.PHONE,
                       u.DIVISION_ID, d.DIVISION_NAME, u.DESIGNATION,
                       u.BUILDING_ID, NULL AS BUILDING_NAME, u.FLOOR_NUMBER,
                       u.IS_ACTIVE, u.LAST_LOGIN_AT, u.CREATED_AT
                FROM   USERS u
                JOIN   DIVISIONS d ON d.DIVISION_ID = u.DIVISION_ID
                WHERE  u.DIVISION_ID = :P_DIV_ID
                  AND  u.IS_ACTIVE = 1
                  AND  u.USER_ID != :P_EXCL
                  AND  (UPPER(u.FULL_NAME) LIKE '%' || UPPER(:P_Q) || '%'
                     OR UPPER(u.USERNAME)  LIKE '%' || UPPER(:P_Q) || '%')
                ORDER BY u.FULL_NAME
                FETCH FIRST 20 ROWS ONLY",
                new[]
                {
                    OracleHelper.ParamInt("P_DIV_ID", divisionId),
                    OracleHelper.ParamInt("P_EXCL", excludeUserId),
                    OracleHelper.ParamStr("P_Q", query)
                });

            var users = new List<User>();
            foreach (DataRow row in dt.Rows)
                users.Add(MapUser(row));
            return users;
        }

        public List<User> GetAll(bool activeOnly = true)
        {
            string sql = @"
                SELECT u.USER_ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.PHONE,
                       u.DIVISION_ID, d.DIVISION_NAME, u.DESIGNATION,
                       u.BUILDING_ID, b.BUILDING_NAME, u.FLOOR_NUMBER,
                       u.IS_ACTIVE, u.LAST_LOGIN_AT, u.CREATED_AT
                FROM   USERS u
                JOIN   DIVISIONS d ON d.DIVISION_ID = u.DIVISION_ID
                LEFT JOIN BUILDINGS b ON b.BUILDING_ID = u.BUILDING_ID
                " + (activeOnly ? "WHERE u.IS_ACTIVE = 1 " : "") + @"
                ORDER BY u.FULL_NAME";

            var dt = OracleHelper.ExecuteQuerySql(sql);
            var users = new List<User>();
            foreach (DataRow row in dt.Rows)
                users.Add(MapUser(row));
            return users;
        }

        public void UpdateLastLogin(int userId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE USERS SET LAST_LOGIN_AT = SYSTIMESTAMP WHERE USER_ID = :P_USER_ID",
                new[] { OracleHelper.ParamInt("P_USER_ID", userId) });
        }

        public int Create(User user, string passwordHash)
        {
            int newId = OracleHelper.NextVal("SEQ_USERS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO USERS (USER_ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, PHONE,
                                   DIVISION_ID, DESIGNATION, BUILDING_ID, FLOOR_NUMBER, IS_ACTIVE)
                VALUES (:P_ID, :P_USER, :P_HASH, :P_NAME, :P_EMAIL, :P_PHONE,
                        :P_DIV, :P_DESIG, :P_BLDG, :P_FLOOR, 1)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID", newId),
                    OracleHelper.ParamStr("P_USER", user.Username),
                    OracleHelper.ParamStr("P_HASH", passwordHash, 256),
                    OracleHelper.ParamStr("P_NAME", user.FullName, 200),
                    OracleHelper.ParamStr("P_EMAIL", user.Email, 254),
                    OracleHelper.ParamStr("P_PHONE", user.Phone, 30),
                    OracleHelper.ParamInt("P_DIV", user.DivisionId),
                    OracleHelper.ParamStr("P_DESIG", user.Designation, 200),
                    OracleHelper.ParamInt("P_BLDG", user.BuildingId),
                    OracleHelper.ParamStr("P_FLOOR", user.FloorNumber, 20)
                });
            return newId;
        }

        public void Update(User user)
        {
            OracleHelper.ExecuteNonQuerySql(@"
                UPDATE USERS SET
                    FULL_NAME   = :P_NAME,
                    EMAIL       = :P_EMAIL,
                    PHONE       = :P_PHONE,
                    DIVISION_ID = :P_DIV,
                    DESIGNATION = :P_DESIG,
                    BUILDING_ID = :P_BLDG,
                    FLOOR_NUMBER= :P_FLOOR,
                    IS_ACTIVE   = :P_ACTIVE,
                    UPDATED_AT  = SYSTIMESTAMP
                WHERE USER_ID = :P_ID",
                new[]
                {
                    OracleHelper.ParamStr("P_NAME", user.FullName, 200),
                    OracleHelper.ParamStr("P_EMAIL", user.Email, 254),
                    OracleHelper.ParamStr("P_PHONE", user.Phone, 30),
                    OracleHelper.ParamInt("P_DIV", user.DivisionId),
                    OracleHelper.ParamStr("P_DESIG", user.Designation, 200),
                    OracleHelper.ParamInt("P_BLDG", user.BuildingId),
                    OracleHelper.ParamStr("P_FLOOR", user.FloorNumber, 20),
                    OracleHelper.ParamBool("P_ACTIVE", user.IsActive),
                    OracleHelper.ParamInt("P_ID", user.UserId)
                });
        }

        public void AssignRole(int userId, int roleId, int? divisionId, int grantedBy)
        {
            // Check not already assigned
            var exists = OracleHelper.ExecuteScalarSql(@"
                SELECT COUNT(*) FROM USER_ROLES
                WHERE USER_ID=:P_U AND ROLE_ID=:P_R
                  AND NVL(DIVISION_ID,-1)=NVL(:P_D,-1) AND IS_ACTIVE=1",
                new[]
                {
                    OracleHelper.ParamInt("P_U", userId),
                    OracleHelper.ParamInt("P_R", roleId),
                    OracleHelper.ParamInt("P_D", divisionId)
                });
            if (Convert.ToInt32(exists) > 0) return;

            int newId = OracleHelper.NextVal("SEQ_USER_ROLES");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO USER_ROLES (USER_ROLE_ID, USER_ID, ROLE_ID, DIVISION_ID, GRANTED_BY)
                VALUES (:P_ID, :P_U, :P_R, :P_D, :P_GB)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID", newId),
                    OracleHelper.ParamInt("P_U", userId),
                    OracleHelper.ParamInt("P_R", roleId),
                    OracleHelper.ParamInt("P_D", divisionId),
                    OracleHelper.ParamInt("P_GB", grantedBy)
                });
        }

        public void RevokeRole(int userRoleId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE USER_ROLES SET IS_ACTIVE = 0 WHERE USER_ROLE_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", userRoleId) });
        }

        // ── Delegations ───────────────────────────────────────────────────────
        public List<RoleDelegation> GetDelegationsByDelegator(int userId)
        {
            var dt = OracleHelper.ExecuteQuerySql(@"
                SELECT rd.*, r.ROLE_NAME,
                       du.FULL_NAME AS DELEGATEE_NAME,
                       d.DIVISION_NAME
                FROM   ROLE_DELEGATIONS rd
                JOIN   ROLES r ON r.ROLE_ID = rd.ROLE_ID
                JOIN   USERS du ON du.USER_ID = rd.DELEGATEE_USER_ID
                LEFT JOIN DIVISIONS d ON d.DIVISION_ID = rd.DIVISION_ID
                WHERE  rd.DELEGATOR_USER_ID = :P_UID
                ORDER BY rd.CREATED_AT DESC",
                new[] { OracleHelper.ParamInt("P_UID", userId) });

            var list = new List<RoleDelegation>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapDelegation(row));
            return list;
        }

        public void CreateDelegation(RoleDelegation d)
        {
            int newId = OracleHelper.NextVal("SEQ_DELEGATIONS");
            OracleHelper.ExecuteNonQuerySql(@"
                INSERT INTO ROLE_DELEGATIONS
                    (DELEGATION_ID, DELEGATOR_USER_ID, DELEGATEE_USER_ID, ROLE_ID,
                     DIVISION_ID, START_DATE, END_DATE, REASON, CREATED_BY)
                VALUES (:P_ID, :P_DLGTR, :P_DLGTE, :P_ROLE,
                        :P_DIV, :P_START, :P_END, :P_REASON, :P_CB)",
                new[]
                {
                    OracleHelper.ParamInt("P_ID", newId),
                    OracleHelper.ParamInt("P_DLGTR", d.DelegatorUserId),
                    OracleHelper.ParamInt("P_DLGTE", d.DelegateeUserId),
                    OracleHelper.ParamInt("P_ROLE", d.RoleId),
                    OracleHelper.ParamInt("P_DIV", d.DivisionId),
                    OracleHelper.ParamDate("P_START", d.StartDate),
                    OracleHelper.ParamDate("P_END", d.EndDate),
                    OracleHelper.ParamStr("P_REASON", d.Reason, 500),
                    OracleHelper.ParamInt("P_CB", d.CreatedBy)
                });
        }

        public void RevokeDelegation(int delegationId)
        {
            OracleHelper.ExecuteNonQuerySql(
                "UPDATE ROLE_DELEGATIONS SET IS_ACTIVE = 0 WHERE DELEGATION_ID = :P_ID",
                new[] { OracleHelper.ParamInt("P_ID", delegationId) });
        }

        // ── Private mappers ───────────────────────────────────────────────────
        private User MapUser(DataRow row) => new User
        {
            UserId        = OracleHelper.ToInt(row["USER_ID"]),
            Username      = OracleHelper.ToString(row["USERNAME"]),
            FullName      = OracleHelper.ToString(row["FULL_NAME"]),
            Email         = OracleHelper.ToString(row["EMAIL"]),
            Phone         = OracleHelper.ToString(row["PHONE"]),
            DivisionId    = OracleHelper.ToInt(row["DIVISION_ID"]),
            DivisionName  = OracleHelper.ToString(row["DIVISION_NAME"]),
            Designation   = OracleHelper.ToString(row["DESIGNATION"]),
            BuildingId    = OracleHelper.ToNullableInt(row["BUILDING_ID"]),
            BuildingName  = OracleHelper.ToString(row["BUILDING_NAME"]),
            FloorNumber   = OracleHelper.ToString(row["FLOOR_NUMBER"]),
            IsActive      = OracleHelper.ToBool(row["IS_ACTIVE"]),
            LastLoginAt   = OracleHelper.ToNullableDateTime(row["LAST_LOGIN_AT"]),
            CreatedAt     = OracleHelper.ToDateTime(row["CREATED_AT"])
        };

        private UserRole MapUserRole(DataRow row) => new UserRole
        {
            UserRoleId   = OracleHelper.ToInt(row["USER_ROLE_ID"]),
            UserId       = OracleHelper.ToInt(row["USER_ID"]),
            RoleId       = OracleHelper.ToInt(row["ROLE_ID"]),
            RoleCode     = OracleHelper.ToString(row["ROLE_CODE"]),
            RoleName     = OracleHelper.ToString(row["ROLE_NAME"]),
            DivisionId   = OracleHelper.ToNullableInt(row["DIVISION_ID"]),
            DivisionName = OracleHelper.ToString(row["DIVISION_NAME"]),
            IsActive     = OracleHelper.ToBool(row["IS_ACTIVE"])
        };

        private RoleDelegation MapDelegation(DataRow row) => new RoleDelegation
        {
            DelegationId     = OracleHelper.ToInt(row["DELEGATION_ID"]),
            DelegatorUserId  = OracleHelper.ToInt(row["DELEGATOR_USER_ID"]),
            DelegateeUserId  = OracleHelper.ToInt(row["DELEGATEE_USER_ID"]),
            DelegateeName    = OracleHelper.ToString(row["DELEGATEE_NAME"]),
            RoleId           = OracleHelper.ToInt(row["ROLE_ID"]),
            RoleName         = OracleHelper.ToString(row["ROLE_NAME"]),
            DivisionId       = OracleHelper.ToNullableInt(row["DIVISION_ID"]),
            DivisionName     = OracleHelper.ToString(row["DIVISION_NAME"]),
            StartDate        = OracleHelper.ToDateTime(row["START_DATE"]),
            EndDate          = OracleHelper.ToDateTime(row["END_DATE"]),
            Reason           = OracleHelper.ToString(row["REASON"]),
            IsActive         = OracleHelper.ToBool(row["IS_ACTIVE"]),
            CreatedAt        = OracleHelper.ToDateTime(row["CREATED_AT"])
        };
    }
}
