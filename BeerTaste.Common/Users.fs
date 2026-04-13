namespace BeerTaste.Common

open System
open System.Threading.Tasks
open Azure.Data.Tables

type User = {
    AuthenticationScheme: string
    AccountId: string
    UserId: Guid
    Name: string
}

module Users =
    let userToEntity (user: User) : TableEntity =
        let entity = TableEntity(user.AuthenticationScheme, user.AccountId)
        entity.Add("UserId", user.UserId)
        entity.Add("Name", user.Name)
        entity

    let entityToUser (entity: TableEntity) : User = {
        AuthenticationScheme = entity.PartitionKey
        AccountId = entity.RowKey
        UserId = entity.GetString("UserId") |> Guid.Parse
        Name = entity.GetString("Name")
    }

    let addUser (usersTable: TableClient) (user: User) : Task =
        task {
            let entity = userToEntity user
            let! _ = usersTable.UpsertEntityAsync(entity)
            ()
        }

    let fetchUser (usersTable: TableClient) (authScheme: string) (accountId: string) : Task<User option> =
        task {
            try
                let! response = usersTable.GetEntityAsync<TableEntity>(authScheme, accountId)
                return response.Value |> entityToUser |> Some
            with :? Azure.RequestFailedException as ex when ex.Status = 404 ->
                return None
        }

    let getOrCreateUser (usersTable: TableClient) (authScheme: string) (accountId: string) (name: string) : Task<User> =
        task {
            let! existing = fetchUser usersTable authScheme accountId

            match existing with
            | Some user -> return user
            | None ->
                let user = {
                    AuthenticationScheme = authScheme
                    AccountId = accountId
                    UserId = Guid.NewGuid()
                    Name = name
                }

                try
                    let entity = userToEntity user
                    let! _ = usersTable.AddEntityAsync(entity)
                    return user
                with :? Azure.RequestFailedException as ex when ex.Status = 409 ->
                    // Lost the race — fetch the winner's record
                    let! existing = fetchUser usersTable authScheme accountId
                    return existing.Value
        }
