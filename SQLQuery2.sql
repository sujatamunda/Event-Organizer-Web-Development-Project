USE [EventOrganizer]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[SP_Login_CURD]
		@TransType = N'INSERT',
		@Email = N'User@gmail.com',
		@Password = N'user123',
		@UserType = N'User'

SELECT	@return_value as 'Return Value'

GO
