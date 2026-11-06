import subprocess
import sys
import logging

logger = logging.getLogger("db_helper")

def run_sqlcmd(query, db_name="master", server="localhost", user="sa", password="123"):
    """Executes a SQL query via sqlcmd utility."""
    cmd = [
        "sqlcmd",
        "-S", server,
        "-U", user,
        "-P", password,
        "-d", db_name,
        "-b",  # On error exit with non-zero
        "-r1", # Send error messages to stderr
        "-Q", f"SET NOCOUNT ON;\n{query}"
    ]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if res.returncode != 0:
        raise RuntimeError(f"sqlcmd error (exit {res.returncode}): {res.stderr}\nOutput: {res.stdout}")
    return res.stdout

def create_database(db_name, server="localhost", user="sa", password="123"):
    """Creates a clean isolated test database (dropping any stale instance with the same name)."""
    safe_db = db_name.replace("'", "''").replace("]", "]]")
    sql = f"""
    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{safe_db}')
    BEGIN
        ALTER DATABASE [{safe_db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{safe_db}];
    END
    CREATE DATABASE [{safe_db}];
    """
    logger.info(f"Creating clean database: {db_name}")
    run_sqlcmd(sql, db_name="master", server=server, user=user, password=password)

def drop_database(db_name, server="localhost", user="sa", password="123"):
    """Drops isolated test database safely."""
    safe_db = db_name.replace("'", "''").replace("]", "]]")
    sql = f"""
    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{safe_db}')
    BEGIN
        ALTER DATABASE [{safe_db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{safe_db}];
    END
    """
    logger.info(f"Dropping database: {db_name}")
    run_sqlcmd(sql, db_name="master", server=server, user=user, password=password)

def query_db_scalar(query, db_name, server="localhost", user="sa", password="123"):
    """Runs a query and returns stripped output."""
    output = run_sqlcmd(f"SET NOCOUNT ON; {query}", db_name=db_name, server=server, user=user, password=password)
    lines = [line.strip() for line in output.splitlines() if line.strip() and not line.startswith("-")]
    return lines[0] if lines else ""

if __name__ == "__main__":
    test_db = "procurement_db_test_probe"
    print("Testing create database...")
    create_database(test_db)
    print("Testing scalar query...")
    count = query_db_scalar("SELECT COUNT(*) FROM sys.tables", db_name=test_db)
    print(f"Table count: {count}")
    print("Testing drop database...")
    drop_database(test_db)
    print("Database helper tests completed successfully!")
