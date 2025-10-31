ALTER PROCEDURE [dbo].[SP_Login_CURD]
    @TransType VARCHAR(50) = NULL,
    @Email     VARCHAR(50) = NULL,
    @Password  VARCHAR(50) = NULL,
    @UserType  NVARCHAR(50) = NULL
AS
BEGIN
    -- LOGIN OPERATION
    IF @TransType = 'SELECT-ONE'
    BEGIN
        SELECT Email, Password, UserType 
        FROM [dbo].[Login] 
        WHERE LOWER(Email) = LOWER(@Email) AND Password = @Password;
    END
END;
