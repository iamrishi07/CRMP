using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;

namespace CRMP.Helpers
{
    /// <summary>
    /// Central Oracle helper — all ADO.NET plumbing lives here so repositories
    /// stay clean.  Uses the Oracle.ManagedDataAccess (ODP.NET Managed) driver.
    /// </summary>
    public static class OracleHelper
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["CRMPConnection"].ConnectionString;

        // ── Connection factory ────────────────────────────────────────────────
        public static OracleConnection GetConnection()
        {
            var conn = new OracleConnection(_connStr);
            conn.Open();
            return conn;
        }

        // ── Execute a stored procedure, returning a DataTable ─────────────────
        public static DataTable ExecuteQuery(string procName, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(procName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);

                // Cursor OUT parameter
                var cursorParam = new OracleParameter("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output);
                cmd.Parameters.Add(cursorParam);

                var da = new OracleDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ── Execute inline SQL, returning a DataTable (for simple lookups) ────
        public static DataTable ExecuteQuerySql(string sql, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                var da = new OracleDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ── Execute non-query stored procedure (INSERT/UPDATE/DELETE) ─────────
        public static void ExecuteNonQuery(string procName, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(procName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        // ── Execute non-query inline SQL ──────────────────────────────────────
        public static void ExecuteNonQuerySql(string sql, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        // ── Execute stored procedure returning a single scalar value ──────────
        public static object ExecuteScalar(string procName, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(procName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);

                var outParam = new OracleParameter("P_RESULT", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);
                cmd.Parameters.Add(outParam);
                cmd.ExecuteNonQuery();
                return outParam.Value;
            }
        }

        // ── Execute inline SQL scalar ─────────────────────────────────────────
        public static object ExecuteScalarSql(string sql, OracleParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        // ── Get next sequence value ───────────────────────────────────────────
        public static int NextVal(string sequenceName)
        {
            var result = ExecuteScalarSql($"SELECT {sequenceName}.NEXTVAL FROM DUAL");
            return Convert.ToInt32(result);
        }

        // ── Safe value readers ────────────────────────────────────────────────
        public static int ToInt(object value, int defaultVal = 0) =>
            value == DBNull.Value || value == null ? defaultVal : Convert.ToInt32(value);

        public static int? ToNullableInt(object value) =>
            value == DBNull.Value || value == null ? (int?)null : Convert.ToInt32(value);

        public static string ToString(object value, string defaultVal = "") =>
            value == DBNull.Value || value == null ? defaultVal : value.ToString();

        public static bool ToBool(object value, bool defaultVal = false) =>
            value == DBNull.Value || value == null ? defaultVal : Convert.ToInt32(value) == 1;

        public static DateTime? ToNullableDateTime(object value) =>
            value == DBNull.Value || value == null ? (DateTime?)null : Convert.ToDateTime(value);

        public static DateTime ToDateTime(object value, DateTime defaultVal = default) =>
            value == DBNull.Value || value == null ? defaultVal : Convert.ToDateTime(value);

        public static decimal ToDecimal(object value, decimal defaultVal = 0) =>
            value == DBNull.Value || value == null ? defaultVal : Convert.ToDecimal(value);

        // ── OracleParameter factories ─────────────────────────────────────────
        public static OracleParameter Param(string name, OracleDbType type, object value) =>
            new OracleParameter(name, type) { Value = value ?? DBNull.Value };

        public static OracleParameter ParamInt(string name, int? value) =>
            new OracleParameter(name, OracleDbType.Int32) { Value = value.HasValue ? (object)value.Value : DBNull.Value };

        public static OracleParameter ParamStr(string name, string value, int size = 4000) =>
            new OracleParameter(name, OracleDbType.Varchar2, size) { Value = (object)value ?? DBNull.Value };

        public static OracleParameter ParamClob(string name, string value) =>
            new OracleParameter(name, OracleDbType.Clob) { Value = (object)value ?? DBNull.Value };

        public static OracleParameter ParamDate(string name, DateTime? value) =>
            new OracleParameter(name, OracleDbType.TimeStamp) { Value = value.HasValue ? (object)value.Value : DBNull.Value };

        public static OracleParameter ParamBool(string name, bool value) =>
            new OracleParameter(name, OracleDbType.Int32) { Value = value ? 1 : 0 };
    }
}
