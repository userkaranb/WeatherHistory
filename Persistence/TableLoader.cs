using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

public interface ITableLoader
{
    Table LoadTable(IAmazonDynamoDB dynamoDbClient, string tableName);
}

public class TableLoader : ITableLoader
{

    public TableLoader()
    {
    }
    public Table LoadTable(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        return Table.LoadTable(dynamoDbClient, tableName);
    }
}
