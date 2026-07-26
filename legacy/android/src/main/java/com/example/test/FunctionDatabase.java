package com.example.test;

import android.content.Context;
import android.os.AsyncTask;

import androidx.annotation.NonNull;
import androidx.room.Database;
import androidx.room.Room;
import androidx.room.RoomDatabase;
import androidx.sqlite.db.SupportSQLiteDatabase;

@Database(entities = {Function.class}, version = 1)
public abstract class FunctionDatabase extends RoomDatabase {

    private static FunctionDatabase instance;

    public abstract FunctionDao functionDao();

    public static synchronized FunctionDatabase getInstance(Context context) {
        if(instance == null) {
            instance = Room.databaseBuilder(context.getApplicationContext(),
                    FunctionDatabase.class, "function")
                    .fallbackToDestructiveMigration()
                    .addCallback(roomCallback)
                    .build();
            SupportSQLiteDatabase db = instance.getOpenHelper().getWritableDatabase();
        }
        return instance;
    }

    private static Callback roomCallback = new Callback() {
        @Override
        public void onCreate(@NonNull SupportSQLiteDatabase db) {
            super.onCreate(db);
            new PopulateDbAsyncTask(instance).execute();
        }
    };

    private static class PopulateDbAsyncTask extends AsyncTask<Void, Void, Void> {
        private FunctionDao functionDao;

        private PopulateDbAsyncTask(FunctionDatabase db) {
            functionDao = db.functionDao();
        }

        @Override
        protected Void doInBackground(Void... voids) {
            functionDao.insert(new Function("costSpeed", 1, 0));
            functionDao.insert(new Function("costMax", 1, 0));
            functionDao.insert(new Function("clearEarn", 1, 0));
            functionDao.insert(new Function("cooltime", 1, 0));
            functionDao.insert(new Function("defeatEarn", 1, 0));
            return null;
        }
    }
}
