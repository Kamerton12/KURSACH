﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using MySql.Data.MySqlClient;
using System.IO;

namespace АРМ_билиотекаря
{
    class DatabaseAdapter
    {
        private readonly object syncLock = new object();
        private static DatabaseAdapter instance;
        MySqlConnection connection;
        public static string connectionStringTemplate = @"server={0};database={1};uid={2};pwd={3};";
        public static string connectionString = @"";
        private DatabaseAdapter()
        {
            connection = new MySqlConnection(connectionString);
        }

        public void createTables()
        {
            lock (syncLock)
            {
                connection.Open();
                MySqlScript script = new MySqlScript(connection, File.ReadAllText(@"create.sql"));
                script.Delimiter = "$$";
                script.Execute();
                connection.Close();
            }
        }

        public void setBDPath(String address, String databaseName, String username, String password)
        {
            connectionString = String.Format(connectionStringTemplate, address, databaseName, username, password);
            connection = new MySqlConnection(connectionString);
        }

        public static DatabaseAdapter getInstance()
        {
            if (instance == null)
            {
                instance = new DatabaseAdapter();
            }
            return instance;
        }

        private void executeQuery(string query)
        {
            MySqlCommand com = new MySqlCommand(query, connection);
            com.ExecuteNonQuery();
        }

        private DataTable formDataTable(string query)
        {
            MySqlDataAdapter da = new MySqlDataAdapter(query, connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public DataTable getFilteredReaders(Reader reader, bool isDate)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "SELECT * FROM reader WHERE name LIKE '%" + reader.name + "%' AND " +
                    "surname LIKE '%" + reader.surname + "%' AND " +
                    "patronymic LIKE '%" + reader.patronymic + "%' AND " +
                    "phone_number LIKE '%" + reader.phone_number + "%' AND " +
                    "id LIKE '%" + reader.id + "%' AND " +
                    "address LIKE '%" + reader.address + "%'";
                if (isDate)
                    query += " AND birthday = '" + reader.birthday.ToString(@"yyyy-MM-dd") + "' ";
                connection.Close();
                return formDataTable(query);
            }
        }

        public void returnBook(int debtId)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "DELETE FROM record WHERE id = " + debtId;
                executeQuery(query);
                connection.Close();
            }
        }

        public DataTable getReaderBooks(int readerId)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "select record.id as record_id, book.id, book.title, CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) as author, language.language, record.issue_date, record.return_date from record inner join book on record.id_book = book.id inner join author on book.id_author = author.id inner join language on book.id_language = language.id where record.id_reader = " + readerId;
                DataTable result = formDataTable(query);
                connection.Close();
                return result;
            }
        }

        public DataTable getFilteredBooks(Book book, string id)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("select " +
                    "book.id, " +
                    "book.title, " +
                    "CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) as author, " +
                    "language.language, " +
                    "genre.genre, " +
                    "publishing_house.title as publishing_house, " +
                    "cover.cover_description as cover, " +
                    "author.id as author_id, " +
                    "language.id as language_id, " +
                    "genre.id as genre_id," +
                    "publishing_house.id as publishing_house_id, " +
                    "cover.id as cover_id " +
                    "from book " +
                    "inner join author on book.id_author = author.id " +
                    "inner join language on book.id_language = language.id " +
                    "inner join genre on book.id_genre = genre.id " +
                    "inner join publishing_house on book.id_publishing_house = publishing_house.id " +
                    "inner join cover on book.id_cover = cover.id " +
                    "WHERE " +
                    "(CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) LIKE '%{0}%') AND " +
                    "(book.title LIKE '%{1}%') AND " +
                    "(language.language LIKE '%{2}%') AND" +
                    "(book.id LIKE '%{3}%')", book.author, book.title, book.language, id);
                //String query = "SELECT * from books WHERE author LIKE '%" + book.author + "%' " +
                //    "AND title LIKE '%" + book.title + "%' " +
                //    "AND book_language LIKE '%" + book.language + "%' " +
                //    "AND location LIKE '%" + book.location + "%' " +
                //    "AND Код LIKE '%" + id + "%'";
                DataTable result = formDataTable(query);
                connection.Close();
                return result;
            }
        }

        public DataTable getFilteredBooksNotTaken(Book book, string id)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("select " +
                   "book.id, book.title, CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) as author, language.language, genre.genre, publishing_house.title as publishing_house, cover.cover_description as cover from book " +
                   "inner join author on book.id_author = author.id " +
                   "inner join language on book.id_language = language.id " +
                   "inner join genre on book.id_genre = genre.id " +
                   "inner join publishing_house on book.id_publishing_house = publishing_house.id " +
                   "inner join cover on book.id_cover = cover.id " +
                   "WHERE " +
                   "(CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) LIKE '%{0}%') AND " +
                   "(book.title LIKE '%{1}%') AND " +
                   "(language.language LIKE '%{2}%') AND" +
                   "(book.id LIKE '%{3}%') AND " +
                   "(book.id NOT IN (select id_book from record))", book.author, book.title, book.language, id);
                //String query = "SELECT * from books WHERE author LIKE '%" + book.author + "%' " +
                //    "AND title LIKE '%" + book.title + "%' " +
                //    "AND book_language LIKE '%" + book.language + "%' " +
                //    "AND Код LIKE '%" + id + "%'" +
                //    //"AND NOT ISNUMERIC(location)";
                //    "AND Код NOT IN (SELECT book_id FROM debtors)";
                DataTable result = formDataTable(query);
                connection.Close();
                return result;
            }
        }

        public void editReader(Reader reader)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "UPDATE reader SET " +
                    "name= '" + reader.name + "'," +
                    "surname= '" + reader.surname + "'," +
                    "patronymic= '" + reader.patronymic + "'," +
                    "birthday= '" + reader.birthday.ToString(@"yyyy-MM-dd") + "'," +
                    "phone_number= '" + reader.phone_number + "'," +
                    "address= '" + reader.address + "'" +
                    "WHERE id =" + reader.id;
                executeQuery(query);
                connection.Close();
            }
        }

        public void addReader(Reader reader)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "INSERT INTO reader (" +
                    "name, " +
                    "surname, " +
                    "patronymic, " +
                    "phone_number, " +
                    "birthday, " +
                    "address) VALUES ('" +
                    reader.name + "','" +
                    reader.surname + "','" +
                    reader.patronymic + "','" +
                    reader.phone_number + "','" +
                    reader.birthday.ToString(@"yyyy-MM-dd") + "','" +
                    reader.address + "')";
                executeQuery(query);
                connection.Close();
            }
        }

        public int getBooksCountFromReader(int id)
        {
            String query = "SELECT COUNT(*) FROM debtors WHERE reader_id = " + id;
            MySqlCommand command = new MySqlCommand(query, connection);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return 0;
            return count;
        }

        public bool isThereBookAtReader(int id)
        {
            lock (syncLock)
            {
                connection.Open();
                bool ans = getBooksCountFromReader(id) != 0;
                connection.Close();
                return ans;
            }
        }

        public DataTable getDebtors()
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "SELECT " +
                    "record.id as id," +
                    "book.id as id_book, " +
                    "record.id_reader as id_reader, " +
                    "reader.name as name, " +
                    "reader.surname as surname, " +
                    "reader.phone_number as phone_number, " +
                    "reader.address as address, " +
                    "book.title as title," +
                    "return_date, " +
                    "issue_date, " +
                    "CONCAT(author.surname, ' ', author.name, ' ', author.patronymic) as author" +
                    " FROM record inner join book on record.id_book = book.id inner join reader on reader.id = record.id_reader inner join author on book.id_author = author.id " +
                    "WHERE (record.return_date <= now())";
                //String query = "SELECT debtors.Код," +
                //    "book_id," +
                //    "reader_id," +
                //    "books.author," +
                //    "books.title," +
                //    "readers.name," +
                //    "readers.surname," +
                //    "readers.phone_number," +
                //    "readers.adress, " +
                //    "issue_date," +
                //    "return_date " +
                //    "FROM (debtors " +
                //    "INNER JOIN books ON debtors.book_id=books.Код) " +
                //    "INNER JOIN readers ON debtors.reader_id=readers.Код " +
                //    "WHERE debtors.return_date <= NOW()";
                DataTable answer = formDataTable(query);
                connection.Close();
                return answer;
            }
        }

        public DataTable deleteReaderAndGetFilteredReaders(Reader reader, bool isDate, int id)
        {
            lock (syncLock)
            {
                connection.Open();
                bool error = false;
                try
                {
                    String query = "DELETE FROM reader WHERE id = " + id;
                    executeQuery(query);
                }
                catch (Exception ex)
                {
                    error = true;
                }
                finally
                {
                    connection.Close();
                }
                if (error)
                {
                    return null;
                }
                else
                {
                    return getFilteredReaders(reader, isDate);
                }
            }
        }

        public void expandIssueDate(int debitId, DateTime newDate)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "UPDATE record SET return_date= '" +
                    newDate.ToString(@"yyyy-MM-dd") + "'" +
                    "WHERE id = " + debitId;
                executeQuery(query);
                connection.Close();
            }
        }

        public void issueBookToReader(int readerId, int bookId, DateTime issueDate, DateTime returnDate)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "INSERT INTO record (" +
                    "id_reader," +
                    "id_book," +
                    "issue_date," +
                    "return_date" +
                    ") VALUES ('" +
                    readerId + "','" +
                    bookId + "'," +
                    "'" + issueDate.ToString(@"yyyy-MM-dd") + "'," +
                    "'" + returnDate.ToString(@"yyyy-MM-dd") + "')";
                executeQuery(query);
                connection.Close();
            }
        }

        public void addBook(string title, int author, int language, int genre, int ph, int cover)
        {
            lock (syncLock)
            {
                connection.Open();

                String query = "INSERT INTO book (" +
                    "title," +
                    "id_author," +
                    "id_language," +
                    "id_genre," +
                    "id_publishing_house," +
                    "id_cover" +
                    ") values ('" + title +
                    "', " + author +
                    ", " + language +
                    ", " + genre +
                    ", " + ph +
                    ", " + cover +
                    ")";
                executeQuery(query);
                connection.Close();
            }
        }

        public void editBook(string title, int author, int language, int genre, int ph, int cover, int id)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("update book set " +
                    "title = '{0}', id_author = '{1}', id_language = '{2}', id_genre = '{3}', id_publishing_house = '{4}', id_cover = '{5}' where id = '{6}'", title, author, language, genre, ph, cover, id);

                executeQuery(query);
                connection.Close();
            }
        }

        public bool deleteBook(int bookId)
        {
            connection.Open();
            bool error = false;
            try
            {
                String query = "DELETE FROM book WHERE id = " + bookId;
                executeQuery(query);
            }
            catch (Exception ex)
            {
                error = true;
            }
            finally
            {
                connection.Close();
            }
            return error;
        }

        public DataTable getCommonData(string tableName)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "SELECT * FROM " + tableName;
                var covers = formDataTable(query);
                connection.Close();
                return covers;
            }
        }
        public DataTable getPublishers()
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "SELECT publishing_house.title, city.city_name, city.id as city_id, publishing_house.id as id FROM publishing_house INNER JOIN city ON publishing_house.id_city = city.id";
                var covers = formDataTable(query);
                connection.Close();
                return covers;
            }
        }

        public DataTable addCommon(string table, string data)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("insert into {0} values(null, '{1}')", table, data);
                executeQuery(query);
                connection.Close();
                return getCommonData(table);
            }
        }
        public DataTable editCommon(string table, string tableName, string value, int id)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("update {0} set {1} = '{2}' where id = {3}", table, tableName, value, id);
                executeQuery(query);
                connection.Close();
                return getCommonData(table);
            }
        }
        public DataTable deleteCommon(string table, int id)
        {
            lock (syncLock)
            {
                connection.Open();
                String query = String.Format("delete from {0} where id = '{1}'", table, id);
                bool error = false;
                try
                {
                    executeQuery(query);
                } 
                catch (Exception ex)
                {
                    error = true;
                }
                finally
                {
                    connection.Close();
                }
                if(error)
                {
                    return null;
                }
                else
                {
                    return getCommonData(table);
                }
            }
        }

        public DataTable authorFullName()
        {
            lock (syncLock)
            {
                connection.Open();
                String query = "SELECT id, CONCAT(surname, ' ', name, ' ', patronymic) as name FROM author";
                var covers = formDataTable(query);
                connection.Close();
                return covers;
            }
        }
    }
}
