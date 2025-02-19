using JJMasterData.Commons.Data;

namespace JJMasterData.Commons.Test.Data;

public class DataAccessTest
{
    private const string TableName = "DataAccessTest";

    private DataAccess DataAccess { get; }

    public DataAccessTest()
    {
        DataAccess = new DataAccess();
        ConfigureSeedValues();
    }
    
    private void ConfigureSeedValues()
    {
        var sql = new StreamReader("Dao/Seed/DataAccessTest.sql").ReadToEnd();
        DataAccess.ExecuteBatch(sql);
    }

    [Theory]
    [InlineData(TableName,true)]
    [InlineData("Foo",false)]
    public async Task TableExistsTest(string table, bool exists)
    {
        Assert.Equal(exists, await DataAccess.TableExistsAsync(table, default));
    }

    [Fact]
    public async Task GetDataTableTest()
    {
        var dataTable = await DataAccess.GetDataTableAsync($"SELECT * FROM {TableName}", default);
        
        Assert.True(dataTable.Rows.Count > 0);
    }
}