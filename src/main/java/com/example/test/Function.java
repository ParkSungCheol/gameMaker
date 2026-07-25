package com.example.test;

import android.widget.ImageView;
import android.widget.TextView;
import androidx.room.Entity;
import androidx.room.Ignore;
import androidx.room.Index;
import androidx.room.PrimaryKey;


@Entity(tableName = "function", indices = {@Index(value = {"name"}, unique = true)})
public class Function {
    // 기본키
    @PrimaryKey(autoGenerate = true)
    private int id;
    // 객체 이름, UNIQUE KEY
    private String name;
    // 적용값
    private int value;
    // upgrade Count
    private int upgradeCount;
    // 객체 이미지
    @Ignore
    private ImageView imageView;
    // 텍스트객체
    @Ignore
    private TextView textView;

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public int getValue() {
        return value;
    }

    public void setValue(int value) {
        this.value = value;
    }

    public int getUpgradeCount() {
        return upgradeCount;
    }

    public void setUpgradeCount(int upgradeCount) {
        this.upgradeCount = upgradeCount;
    }

    public ImageView getImageView() {
        return imageView;
    }

    public void setImageView(ImageView imageView) {
        this.imageView = imageView;
    }

    public TextView getTextView() {
        return textView;
    }

    public void setTextView(TextView textView) {
        this.textView = textView;
    }

    public Function(String name, int value, int upgradeCount) {
        this.name = name;
        this.value = value;
        this.upgradeCount = upgradeCount;
        this.imageView = null;
        this.textView = null;
    }
}
