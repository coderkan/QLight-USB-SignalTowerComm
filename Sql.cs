using System;

public class Sql
{
    public String Database { get; set; }
    public String Server { get; set; }
    public String User { get; set; }
    public String Password { get; set; }

    public Sql()
	{

	}

    public Sql(String srvr, String db, String usr, String psw)
    {
        Server = srvr;
        Database = db;
        User = usr;
        Password = psw;
    }

    public Connect()
    {
     
    }
}
