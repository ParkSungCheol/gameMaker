package com.example.test;

import android.content.Context;
import android.os.AsyncTask;

import androidx.annotation.NonNull;
import androidx.room.Database;
import androidx.room.Room;
import androidx.room.RoomDatabase;
import androidx.sqlite.db.SupportSQLiteDatabase;

import java.util.List;

@Database(entities = {Monster.class}, version = 1)
public abstract class MonsterDatabase extends RoomDatabase {

    private static MonsterDatabase instance;

    public abstract MonsterDao monsterDao();

    public static synchronized MonsterDatabase getInstance(Context context) {
        if(instance == null) {
            instance = Room.databaseBuilder(context.getApplicationContext(),
                    MonsterDatabase.class, "monster")
                    .fallbackToDestructiveMigration()
                    .addCallback(roomCallback)
                    .build();
            SupportSQLiteDatabase db = instance.getOpenHelper().getWritableDatabase();
        }
        return instance;
    }

    private static RoomDatabase.Callback roomCallback = new RoomDatabase.Callback() {
        @Override
        public void onCreate(@NonNull SupportSQLiteDatabase db) {
            super.onCreate(db);
            new PopulateDbAsyncTask(instance).execute();
        }
    };

    private static class PopulateDbAsyncTask extends AsyncTask<Void, Void, Void> {
        private MonsterDao monsterDao;

        private PopulateDbAsyncTask(MonsterDatabase db) {
            monsterDao = db.monsterDao();
        }

        @Override
        protected Void doInBackground(Void... voids) {
            monsterDao.insert(new Monster("ourcastle", 100, 0, 0, 0L, 0L, 0, 0, 0, 0, 0));
            monsterDao.insert(new Monster("ourbasic", 100, 10, 300, 12000L, 2000L, 0, 0, 0, 0, 20));
            monsterDao.insert(new Monster("ourtank", 200, 5, 200, 10000L, 4000L, 0, 1, 300, 0, 20));
            monsterDao.insert(new Monster("ourbattle", 100, 10, 400, 15000L, 2000L, 0, 2, 100, 0, 50));
            monsterDao.insert(new Monster("ourmass", 100, 5, 200, 17000L, 1000L, 0, 1, 300, 50, 50));
            monsterDao.insert(new Monster("yourcastle", 100, 0, 0, 0L, 0L, 0, 0, 0, 0, 0));
            monsterDao.insert(new Monster("yourbasic", 100, 10, 300, 12000L, 2000L, 0, 0, 0, 0, 20));
            monsterDao.insert(new Monster("yourtank", 200, 5, 200, 10000L, 4000L, 0, 1, 300, 0, 20));
            monsterDao.insert(new Monster("yourbattle", 100, 10, 400, 15000L, 2000L, 0, 2, 100, 0, 50));
            monsterDao.insert(new Monster("yourboss", 999999, 50, 200, 15000L, 1000L, 0, 0, 0, 0, 0));
            monsterDao.insert(new Monster("yourmass", 100, 5, 200, 17000L, 1000L, 0, 1, 300, 50, 50));
            return null;
        }
    }
}
